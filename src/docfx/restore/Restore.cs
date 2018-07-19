// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Restore
    {
        public static async Task Run(string docsetPath, CommandLineOptions options)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            using (Progress.Start("Restore dependencies"))
            {
                var restoredDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);

                await RestoreDocset(docsetPath);

                async Task RestoreDocset(string docset)
                {
                    if (restoredDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out _, false))
                    {
                        await RestoreLocker.Save(docset, () => RestoreOneDocset(docset, options, RestoreDocset, options.GitToken));
                    }
                }
            }

            using (Progress.Start("GC dependencies"))
            {
                var gcDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);

                await GCDocset(docsetPath);

                async Task GCDocset(string docset)
                {
                    if (gcDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out var docsetConfig))
                    {
                        await GCOneDocset(docsetConfig, GCDocset);
                    }
                }
            }
        }

        public static string GetRestoreRootDir(string url, string root)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var uri = new Uri(url);
            var repo = Path.Combine(uri.Host, uri.AbsolutePath.Substring(1));
            var dir = Path.Combine(root, repo);

            // todo: encode the dir converted from url
            return PathUtility.NormalizeFolder(dir);
        }

        private static IEnumerable<string> GetRestorePaths(Config config)
        {
            yield return config.Contribution.GitCommitsTime;
            yield return config.Contribution.UserProfileCache;
        }

        private static IEnumerable<string> GetRestoreUrls(IEnumerable<string> paths)
        {
            foreach (var url in paths)
            {
                if (!string.IsNullOrEmpty(url) && HrefUtility.IsAbsoluteHref(url))
                {
                    yield return url;
                }
            }
        }

        private static async Task<RestoreLock> RestoreOneDocset(string docsetPath, CommandLineOptions options, Func<string, Task> restoreChild, string token)
        {
            var restoreLock = new RestoreLock();

            // restore extend url firstly
            // no need to extend config
            var config = Config.Load(docsetPath, options, false);
            var restoreUrlMappings = new ConcurrentDictionary<string, string>();
            await ParallelUtility.ForEach(
                GetRestoreUrls(config.Extend),
                async restoreUrl =>
                {
                    restoreUrlMappings[restoreUrl] = await RestoreUrl.Restore(restoreUrl);
                });
            restoreLock.Url = restoreUrlMappings.ToDictionary(k => k.Key, v => v.Value);

            // restore other urls and git dependnecy repositories
            // extend the config before loading
            config = Config.Load(docsetPath, options, true, new RestoreMap(restoreLock));
            var workTreeHeadMappings = await RestoreGit.Restore(config, restoreChild, token);
            foreach (var (href, workTreeHead) in workTreeHeadMappings)
            {
                restoreLock.Git[href] = workTreeHead;
            }

            await ParallelUtility.ForEach(
            GetRestoreUrls(GetRestorePaths(config)),
            async restoreUrl =>
            {
                restoreUrlMappings[restoreUrl] = await RestoreUrl.Restore(restoreUrl);
            });

            restoreLock.Url = restoreUrlMappings.ToDictionary(k => k.Key, v => v.Value);

            return restoreLock;
        }

        private static async Task GCOneDocset(Config config, Func<string, Task> gcChild)
        {
            await RestoreGit.GC(config, gcChild);

            var restoreUrls = GetRestoreUrls(GetRestorePaths(config).Concat(config.Extend));
            await ParallelUtility.ForEach(restoreUrls, async restoreUrl =>
            {
                await RestoreUrl.GC(restoreUrl);
            });
        }
    }
}
