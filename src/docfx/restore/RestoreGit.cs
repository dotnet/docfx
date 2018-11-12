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
            => Docs.Build.Restore.GetRestoreRootDir(url, AppData.GitDir);

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
            var gitDependencies = config.Dependencies.Values.Concat(GetLocRestoreItem(docsetPath, config, locale));

            return gitDependencies.GroupBy(d => GetRestoreRootDir(d), PathUtility.PathComparer).Select(g => (g.Key, g.Distinct().ToList())).ToList();
        }

        private static IEnumerable<string> GetLocRestoreItem(string docsetPath, Config config, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            if (string.Equals(locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (config.Localization.Mapping != LocalizationMapping.Repository && config.Localization.Mapping != LocalizationMapping.RepositoryAndFolder)
            {
                yield break;
            }

            var repo = Repository.CreateFromFolder(Path.GetFullPath(docsetPath));
            if (repo == null)
            {
                yield break;
            }

            if (config.Localization.Bilingual)
            {
                // Bilingual repos also depend on non bilingual branch
                yield return ToHref(LocalizationConvention.GetLocalizationRepo(
                    config.Localization.Mapping,
                    bilingual: false,
                    repo.Remote,
                    repo.Branch,
                    locale,
                    config.Localization.DefaultLocale));
            }

            yield return ToHref(LocalizationConvention.GetLocalizationRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repo.Remote,
                repo.Branch,
                locale,
                config.Localization.DefaultLocale));

            string ToHref((string url, string branch) value)
            {
                return string.Concat(value.url, "#", value.branch);
            }
        }
    }
}
