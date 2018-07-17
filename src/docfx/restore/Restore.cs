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
        public static async Task Run(string docsetPath, CommandLineOptions options, Report report)
        {
            // Restore has to use Config directly, it cannot depend on Docset,
            // because Docset assumes the repo to physically exist on disk.
            var config = Config.Load(docsetPath, options);
            report.Configure(docsetPath, config);

            using (Progress.Start("Restore dependencies"))
            {
                var restoredDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);

                await RestoreDocset(docsetPath);

                async Task RestoreDocset(string docset)
                {
                    if (restoredDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out var docsetConfig))
                    {
                        await RestoreLocker.Save(docset, () => RestoreOneDocset(docset, docsetConfig, RestoreDocset));
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

        private static IEnumerable<string> GetRestoreUrls(Config config)
        {
            var restoreUrls = new[]
            {
                config.Contribution.UserProfileCache,
                config.Contribution.GitCommitsTime,
            };

            foreach (var url in restoreUrls)
            {
                if (!string.IsNullOrEmpty(url) && HrefUtility.IsAbsoluteHref(url))
                {
                    yield return url;
                }
            }
        }

        private static async Task<RestoreLock> RestoreOneDocset(string docsetPath, Config config, Func<string, Task> restoreChild)
        {
            var result = new RestoreLock();

            // restore git dependnecy repositories
            var workTreeHeadMappings = await RestoreGit.Restore(docsetPath, config, restoreChild);
            foreach (var (href, workTreeHead) in workTreeHeadMappings)
            {
                result.Git[href] = workTreeHead;
            }

            var restoreUrls = GetRestoreUrls(config);
            var restoreUrlMappings = new ConcurrentDictionary<string, string>();
            await ParallelUtility.ForEach(restoreUrls, async restoreUrl =>
            {
                restoreUrlMappings[restoreUrl] = await RestoreUrl.Restore(docsetPath, restoreUrl);
            });

            result.Url = restoreUrlMappings.ToDictionary(k => k.Key, v => v.Value);
            return result;
        }

        private static async Task GCOneDocset(Config config, Func<string, Task> gcChild)
        {
            await RestoreGit.GC(config, gcChild);

            var restoreUrls = GetRestoreUrls(config);
            await ParallelUtility.ForEach(restoreUrls, async restoreUrl =>
            {
                await RestoreUrl.GC(restoreUrl);
            });
        }
    }
}
