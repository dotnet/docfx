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
            using (Progress.Start("Restore dependencies"))
            {
                // Restore has to use Config directly, it cannot depend on Docset,
                // because Docset assumes the repo to physically exist on disk.
                var config = Config.Load(docsetPath, options);

                report.Configure(docsetPath, config);

                var restoredDocsets = new ConcurrentDictionary<string, int>(PathUtility.PathComparer);

                await RestoreDocset(docsetPath);

                async Task RestoreDocset(string docset)
                {
                    if (restoredDocsets.TryAdd(docset, 0) && Config.LoadIfExists(docset, options, out var docsetConfig))
                    {
                        // todo: Parallel competition issue for "get lock" and then "save lock"
                        var docsetLock = await RestoreOneDocset(docset, docsetConfig, RestoreDocset);
                        await RestoreLocker.Save(docset, docsetLock);
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

        private static List<string> GetRestoreUrls(Config config)
        {
            var restoreUrls = new List<string>();

            // restore github user
            if (!string.IsNullOrEmpty(config.Contribution.UserProfileCache) && HrefUtility.IsAbsoluteHref(config.Contribution.UserProfileCache))
            {
                restoreUrls.Add(config.Contribution.UserProfileCache);
            }

            // restore commit last update at
            if (!string.IsNullOrEmpty(config.Contribution.GitCommitsTime) && HrefUtility.IsAbsoluteHref(config.Contribution.GitCommitsTime))
            {
                restoreUrls.Add(config.Contribution.GitCommitsTime);
            }

            return restoreUrls;
        }
    }
}
