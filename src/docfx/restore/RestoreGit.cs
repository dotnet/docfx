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
        public static Task Restore(string docsetPath, Config config, Func<string, Task> restoreChild, string locale)
        {
            var gitDependencies =
                from git in GetGitDependencies(docsetPath, config, locale)
                group git.branch
                by git.remote;

            return ParallelUtility.ForEach(gitDependencies, RestoreGitRepo, Progress.Update);

            async Task RestoreGitRepo(IGrouping<string, string> group)
            {
                var remote = group.Key;
                var branches = group.Distinct().ToArray();
                var repoPath = Path.GetFullPath(Path.Combine(AppData.GetGitDir(remote), ".git"));
                var childRepos = new List<string>();

                await ProcessUtility.RunInsideMutex(
                    remote,
                    async () =>
                    {
                        try
                        {
                            await GitUtility.CloneOrUpdateBare(repoPath, remote, branches, config);
                            await AddWorkTrees();
                        }
                        catch (Exception ex)
                        {
                            throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                        }
                    });

                foreach (var child in childRepos)
                {
                    await restoreChild(child);
                }

                async Task AddWorkTrees()
                {
                    var existingWorkTreePath = new ConcurrentHashSet<string>(await GitUtility.ListWorkTree(repoPath));

                    await ParallelUtility.ForEach(branches, async branch =>
                    {
                        // contribution branch no need to be checked out
                        if (branches.Any(b => LocalizationConvention.TryGetContributionBranch(b, out var cb) && cb == branch))
                        {
                            return;
                        }

                        // use branch name instead of commit hash
                        // https://git-scm.com/docs/git-worktree#_commands
                        var workTreeHead = $"{HrefUtility.EscapeUrlSegment(branch)}-{GitUtility.RevParse(repoPath, branch)}";
                        var workTreePath = Path.GetFullPath(Path.Combine(repoPath, "../", workTreeHead)).Replace('\\', '/');

                        if (existingWorkTreePath.TryAdd(workTreePath))
                        {
                            await GitUtility.AddWorkTree(repoPath, branch, workTreePath);
                        }

                        childRepos.Add(workTreePath);

                        // update the last write time
                        Directory.SetLastWriteTimeUtc(workTreePath, DateTime.UtcNow);
                    });
                }
            }
        }

        private static IEnumerable<(string remote, string branch)> GetGitDependencies(string docsetPath, Config config, string locale)
        {
            return config.Dependencies.Values.Select(HrefUtility.SplitGitHref)
                         .Concat(GetLocalizationGitDependencies(docsetPath, config, locale))
                         .Concat(GetThemeGitDependencies(config, locale));
        }

        private static IEnumerable<(string remote, string branch)> GetThemeGitDependencies(Config config, string locale)
        {
            if (string.IsNullOrEmpty(config.Theme))
            {
                yield break;
            }

            yield return LocalizationConvention.GetLocalizationTheme(config.Theme, locale, config.Localization.DefaultLocale);
        }

        private static IEnumerable<(string remote, string branch)> GetLocalizationGitDependencies(string docsetPath, Config config, string locale)
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
                yield return LocalizationConvention.GetLocalizationRepo(
                    config.Localization.Mapping,
                    bilingual: false,
                    repo.Remote,
                    repo.Branch,
                    locale,
                    config.Localization.DefaultLocale);
            }

            yield return LocalizationConvention.GetLocalizationRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repo.Remote,
                repo.Branch,
                locale,
                config.Localization.DefaultLocale);
        }
    }
}
