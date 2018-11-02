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

        public static async Task<IEnumerable<(string href, string workTreeHead)>> Restore(string docsetPath, Config config, Func<string, Task> restoreChild, string locale)
        {
            var workTreeMappings = new ConcurrentBag<(string href, string workTreeHead)>();

            // process git restore items
            await ParallelUtility.ForEach(
               GetRestoreItems(docsetPath, config, locale),
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
                var workTreeHeads = await RestoreWorkTree.AddWorkTrees(restoreDir, hrefs, config);

                foreach (var (_, workTreeHead) in workTreeHeads)
                {
                    var childDir = RestoreWorkTree.GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                    await restoreChild(childDir);
                }

                return workTreeHeads;
            }
        }

        private static List<(string restoreDir, List<string> hrefs)> GetRestoreItems(string docsetPath, Config config, string locale)
        {
            // restore dependency repositories
            var restoreItems = config.Dependencies.Values.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => (g.Key, g.Distinct().ToList())).ToList();

            // restore loc repository
            var (locRestoreDir, locRepoHref) = GetLocRestoreItem(docsetPath, config, locale);
            if (!string.IsNullOrEmpty(locRepoHref) && !string.IsNullOrEmpty(locRestoreDir))
            {
                restoreItems.Add((locRestoreDir, new List<string> { locRepoHref }));
            }

            return restoreItems;
        }

        private static (string locRestoreDir, string href) GetLocRestoreItem(string docsetPath, Config config, string locale)
        {
            // restore loc repository
            if (string.IsNullOrEmpty(locale))
            {
                return default;
            }

            if (string.Equals(locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                return default;
            }

            if (config.Localization.Mapping != LocalizationMapping.Repository && config.Localization.Mapping != LocalizationMapping.RepositoryAndFolder)
            {
                return default;
            }

            var repo = Repository.CreateFromFolder(Path.GetFullPath(docsetPath));
            if (repo == null)
            {
                return default;
            }

            var (locRemote, locBranch) = LocalizationConvention.GetLocalizationRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repo.Remote,
                repo.Branch,
                locale,
                config.Localization.DefaultLocale);
            var locRepoUrl = $"{locRemote}#{locBranch}";

            return (GetRestoreRootDir(locRepoUrl), locRepoUrl);
        }
    }
}
