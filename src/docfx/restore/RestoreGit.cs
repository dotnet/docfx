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

        public static async Task Restore(string docsetPath, Config config, Func<string, Task> restoreChild, string locale, bool @implicit, bool isDependencyRepo)
        {
            var gitDependencies =
                from git in GetGitDependencies(docsetPath, config, locale, isDependencyRepo)
                group (git.branch, git.flags)
                by git.remote;

            var workTrees = new List<string>();

            await ParallelUtility.ForEach(gitDependencies, async group => { workTrees.AddRange(await RestoreGitRepo(group)); }, Progress.Update);

            foreach (var workTree in workTrees)
            {
                // update the last write time
                Directory.SetLastWriteTimeUtc(workTree, DateTime.UtcNow);
            }

            if (!isDependencyRepo && LocalizationUtility.TryGetContributionBranch(docsetPath, out var contributionBranch, out var repo))
            {
                await GitUtility.Fetch(repo.Path, repo.Remote, contributionBranch, config);
            }

            async Task<List<string>> RestoreGitRepo(IGrouping<string, (string branch, GitFlags flags)> group)
            {
                var worktreePaths = new List<string>();
                var remote = group.Key;
                var branches = group.Select(g => g.branch).ToArray();
                var depthOne = group.All(g => (g.flags & GitFlags.DepthOne) != 0);
                var branchesToFetch = new HashSet<string>(branches);

                if (@implicit)
                {
                    foreach (var branch in branches)
                    {
                        if (RestoreMap.TryGetGitRestorePath(remote, branch, out var existingPath))
                        {
                            branchesToFetch.Remove(branch);
                            worktreePaths.Add(existingPath);
                        }
                    }
                }

                var repoPath = Path.GetFullPath(Path.Combine(AppData.GetGitDir(remote), ".git"));
                var childRepos = new List<string>();

                await ProcessUtility.RunInsideMutex(
                    remote,
                    async () =>
                    {
                        if (branchesToFetch.Count > 0)
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

                foreach (var worktree in worktreePaths)
                {
                    await restoreChild(worktree);
                }

                return worktreePaths;

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

                        worktreePaths.Add(workTreePath);
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
                dependencies = dependencies.Concat(GetLocalizationGitDependencies(docsetPath, config, locale));
            }

            return dependencies;
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetThemeGitDependencies(Config config, string locale)
        {
            if (string.IsNullOrEmpty(config.Theme))
            {
                yield break;
            }

            var (remote, branch) = LocalizationUtility.GetLocalizedTheme(config.Theme, locale, config.Localization.DefaultLocale);

            yield return (remote, branch, GitFlags.DepthOne);
        }

        /// <summary>
        /// Get source repository or localized repository
        /// </summary>
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

            var repo = Repository.Create(docsetPath);
            if (repo == null || string.IsNullOrEmpty(repo.Remote))
            {
                yield break;
            }

            if (LocalizationUtility.TryGetSourceRepository(repo.Remote, repo.Branch, out var sourceRemote, out var sourceBranch, out var l))
            {
                yield return (sourceRemote, sourceBranch, GitFlags.None);
                yield break; // no need to find localized repo anymore
            }

            if (config.Localization.Mapping == LocalizationMapping.Folder)
            {
                yield break;
            }

            var (remote, branch) = LocalizationUtility.GetLocalizedRepo(
                config.Localization.Mapping,
                config.Localization.Bilingual,
                repo.Remote,
                repo.Branch,
                locale,
                config.Localization.DefaultLocale);

            yield return (remote, branch, GitFlags.None);

            if (config.Localization.Bilingual && LocalizationUtility.TryGetContributionBranch(branch, out var contributionBranch))
            {
                // Bilingual repos also depend on non bilingual branch for commit history
                yield return (remote, contributionBranch, GitFlags.NoCheckout);
            }
        }
    }
}
