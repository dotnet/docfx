// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class RestoreGit
    {
        public static string GetRestoreRootDir(string url)
            => Docs.Build.Restore.GetRestoreRootDir(url, AppData.GitRestoreDir);

        public static async Task<IEnumerable<(string href, string workTreeHead)>> Restore(string docsetPath, Config config, Func<string, Task> restoreChild, string token, string locale)
        {
            var workTreeMappings = new ConcurrentBag<(string href, string workTreeHead)>();

            // process git restore items
            await ParallelUtility.ForEach(
               GetRestoreItems(),
               async restoreItem =>
               {
                   var (restoreDir, hrefs) = restoreItem;
                   var workTreeHeads = await RestoreGitRepo(restoreDir, hrefs);
                   foreach (var workTreeHead in workTreeHeads)
                   {
                       workTreeMappings.Add(workTreeHead);
                   }
               },
               progress: Progress.Update);

            return workTreeMappings;

            async Task<List<(string href, string head)>> RestoreGitRepo(string restoreDir, List<string> hrefs)
            {
                var workTreeHeads = await RestoreWorkTree.AddWorkTrees(restoreDir, hrefs, token);

                foreach (var (_, workTreeHead) in workTreeHeads)
                {
                    var childDir = RestoreWorkTree.GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    await restoreChild(childDir);
                }

                return workTreeHeads;
            }

            List<(string restoreDir, List<string> hrefs)> GetRestoreItems()
            {
                // restore dependency repositories
                var restoreItems = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => (g.Key, g.Distinct().ToList())).ToList();

                // restore loc repository
                var (locRestoreDir, locRepoHref) = GetLocRestoreItem();
                if (!string.IsNullOrEmpty(locRepoHref) && !string.IsNullOrEmpty(locRestoreDir))
                {
                    restoreItems.Add((locRestoreDir, new List<string> { locRepoHref }));
                }

                return restoreItems;
            }

            (string locRestoreDir, string href) GetLocRestoreItem()
            {
                // restore loc repository
                if (string.IsNullOrEmpty(locale))
                {
                    return default;
                }

                if (string.Equals(locale, config.DefaultLocale, StringComparison.OrdinalIgnoreCase))
                {
                    return default;
                }

                if (config.LocMappingType != LocMappingType.Repository && config.LocMappingType != LocMappingType.RepositoryAndFolder)
                {
                    return default;
                }

                var repo = Repository.CreateFromFolder(Path.GetFullPath(docsetPath));
                if (repo == null)
                {
                    return default;
                }

                var (locRemote, _) = LocConfigConvention.GetLocRepository(config.LocMappingType, repo.Remote, locale, config.DefaultLocale);
                var locRepoUrl = $"{locRemote}#{repo.Branch}";

                return (GetRestoreRootDir(locRepoUrl), locRepoUrl);
            }
        }

        public static async Task GC(Config config, Func<string, Task> gcChild)
        {
            var restoreDirs = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => g.Key);

            await ParallelUtility.ForEach(
               restoreDirs,
               async restoreDir =>
               {
                   var leftWorkTrees = await RestoreWorkTree.CleanupWorkTrees(restoreDir);
                   foreach (var leftWorkTree in leftWorkTrees)
                   {
                       await gcChild(leftWorkTree);
                   }
               },
               progress: Progress.Update);
        }
    }
}
