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
        [Flags]
        private enum GitFlags
        {
            None = 0,
            NoCheckout = 1 << 1,
            DepthOne = 1 << 2,
        }

        public static Task Restore(string docsetPath, Config config, Func<string, Task> restoreChild, string locale, bool @implicit, bool isDependencyRepo)
        {
            var gitDependencies =
                from git in GetGitDependencies(docsetPath, config, locale, isDependencyRepo)
                group (git.branch, git.flags)
                by git.remote;

            return ParallelUtility.ForEach(gitDependencies, RestoreGitRepo, Progress.Update);

            async Task RestoreGitRepo(IGrouping<string, (string branch, GitFlags flags)> group)
            {
                var remote = group.Key;
                var branches = group.Select(g => g.branch).Distinct().ToArray();
                var depthOne = group.All(g => (g.flags & GitFlags.DepthOne) != 0);
                var branchesToFetch = @implicit
                    ? branches.Where(branch => !RestoreMap.TryGetGitRestorePath(remote, branch, out _)).ToArray()
                    : branches;

                var repoPath = Path.GetFullPath(Path.Combine(AppData.GetGitDir(remote), ".git"));
                var childRepos = new List<string>();

                await ProcessUtility.RunInsideMutex(
                    remote,
                    async () =>
                    {
                        if (branchesToFetch.Length > 0)
                        {
                            try
                            {
                                await GitUtility.CloneOrUpdateBare(repoPath, remote, branchesToFetch, depthOne, config);
                            }
                            catch (Exception ex)
                            {
                                throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                            }
                            await AddWorkTrees();
                        }
                    });

                foreach (var branch in branches)
                {
                    await restoreChild(RestoreMap.GetGitRestorePath(remote, branch));
                }

                async Task AddWorkTrees()
                {
                    var existingWorkTreePath = new ConcurrentHashSet<string>(await GitUtility.ListWorkTree(repoPath));

                    await ParallelUtility.ForEach(branchesToFetch, async branch =>
                    {
                        var nocheckout = group.Where(g => g.branch == branch).All(g => (g.flags & GitFlags.NoCheckout) != 0);
                        if (nocheckout)
                        {
                            return;
                        }

                        // use branch name instead of commit hash
                        // https://git-scm.com/docs/git-worktree#_commands
                        var workTreeHead = $"{HrefUtility.EscapeUrlSegment(branch)}-{GitUtility.RevParse(repoPath, branch)}";
                        var workTreePath = Path.GetFullPath(Path.Combine(repoPath, "../", workTreeHead)).Replace('\\', '/');

                        if (existingWorkTreePath.TryAdd(workTreePath))
                        {
                            try
                            {
                                await GitUtility.AddWorkTree(repoPath, branch, workTreePath);
                            }
                            catch (Exception ex)
                            {
                                throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                            }
                        }

                        // update the last write time
                        Directory.SetLastWriteTimeUtc(workTreePath, DateTime.UtcNow);
                    });
                }
            }
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetGitDependencies(string docsetPath, Config config, string locale, bool isDependencyRepo)
        {
            var dependencies = config.Dependencies.Values.Select(url =>
            {
                var (remote, branch) = HrefUtility.SplitGitHref(url);
                return (remote, branch, GitFlags.DepthOne);
            });

            dependencies = dependencies.Concat(GetThemeGitDependencies(config, locale));

            if (!isDependencyRepo)
            {
                dependencies = dependencies
                    .Concat(GetSourceGitDependencies(docsetPath, config, locale))
                    .Concat(GetLocalizationGitDependencies(docsetPath, config, locale));
            }

            return dependencies;
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetThemeGitDependencies(Config config, string locale)
        {
            if (string.IsNullOrEmpty(config.Theme))
            {
                yield break;
            }

            var (remote, branch) = LocalizationConvention.GetLocalizationTheme(config.Theme, locale, config.Localization.DefaultLocale);

            yield return (remote, branch, GitFlags.DepthOne);
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetLocalizationGitDependencies(string docsetPath, Config config, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            if (string.Equals(locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            if (config.Localization.Mapping == LocalizationMapping.Folder)
            {
                yield break;
            }

            var repo = Repository.Create(Path.GetFullPath(docsetPath));
            if (repo == null)
            {
                yield break;
            }

            var (remote, branch) = LocalizationConvention.GetLocalizationRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repo.Remote,
                repo.Branch,
                locale,
                config.Localization.DefaultLocale);

            yield return (remote, branch, GitFlags.None);

            if (config.Localization.Bilingual && LocalizationConvention.TryGetContributionBranch(branch, out var contributionBranch))
            {
                // Bilingual repos also depend on non bilingual branch for commit history
                yield return (remote, contributionBranch, GitFlags.NoCheckout);
            }
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetSourceGitDependencies(string docsetPath, Config config, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            var repo = Repository.Create(docsetPath);
            if (repo == null || string.IsNullOrEmpty(repo.Remote))
            {
                yield break;
            }

            if (LocalizationConvention.TryGetSourceRepository(repo.Remote, repo.Branch, config.Localization.DefaultLocale, out var sourceRemote, out var sourceBranch, out _))
            {
                yield return (sourceRemote, sourceBranch, GitFlags.None);
            }
        }
    }
}
