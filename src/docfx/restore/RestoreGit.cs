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
    internal static class RestoreGit
    {
        [Flags]
        private enum GitFlags
        {
            None = 0,
            NoCheckout = 1 << 1,
            DepthOne = 1 << 2,
        }

        public static async Task Restore(
            Config config,
            Func<string, DependencyLock, Task> restoreChild,
            string locale,
            bool @implicit,
            Repository rootRepository,
            DependencyLock dependencyLock)
        {
            var gitDependencies =
                from git in GetGitDependencies(config, locale, rootRepository)
                group (git.branch, git.flags)
                by git.remote;

            var children = new ConcurrentBag<(string workTree, DependencyLock dependencyLock)>();

            // restore first level children
            await ParallelUtility.ForEach(
                gitDependencies,
                async group =>
                {
                    foreach (var child in await RestoreGitRepo(group))
                    {
                        children.Add(child);
                    }
                },
                Progress.Update);

            // update the last write time
            foreach (var child in children)
            {
                Directory.SetLastWriteTimeUtc(child.workTree, DateTime.UtcNow);
            }

            // fetch contribution branch
            if (rootRepository != null && LocalizationUtility.TryGetContributionBranch(rootRepository, out var contributionBranch))
            {
                await GitUtility.Fetch(rootRepository.Path, rootRepository.Remote, contributionBranch, config);
            }

            // restore sub-level children
            foreach (var child in children)
            {
                await restoreChild(child.workTree, child.dependencyLock);
            }

            async Task<List<(string workTree, DependencyLock dependencyLock)>> RestoreGitRepo(IGrouping<string, (string branch, GitFlags flags)> group)
            {
                var subChildren = new List<(string workTree, DependencyLock dependencyLock)>();
                var remote = group.Key;
                var branches = group.Select(g => g.branch).ToArray();
                var depthOne = group.All(g => (g.flags & GitFlags.DepthOne) != 0) && !(dependencyLock?.ContainsGitLock(remote) ?? false);
                var branchesToFetch = new HashSet<string>(branches);

                if (@implicit)
                {
                    foreach (var branch in branches)
                    {
                        if (RestoreMap.TryGetGitRestorePath(remote, branch, dependencyLock, out var existingPath, out var subDependencyLock))
                        {
                            {
                                branchesToFetch.Remove(branch);
                                subChildren.Add((existingPath, subDependencyLock));
                            }
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

                return subChildren;

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

                        var headCommit = GitUtility.RevParse(repoPath, branch);
                        if (string.IsNullOrEmpty(headCommit))
                        {
                            throw Errors.CommittishNotFound(remote, branch).ToException();
                        }

                        var gitDependencyLock = dependencyLock?.GetGitLock(remote, branch);
                        headCommit = gitDependencyLock?.Commit ?? headCommit;

                        var workTreeHead = $"{GetWorkTreeHeadPrefix(branch, !string.IsNullOrEmpty(gitDependencyLock?.Commit))}{headCommit}";
                        var workTreePath = Path.GetFullPath(Path.Combine(repoPath, "../", workTreeHead)).Replace('\\', '/');

                        if (existingWorkTreePath.TryAdd(workTreePath))
                        {
                            try
                            {
                                await GitUtility.AddWorkTree(repoPath, headCommit, workTreePath);
                            }
                            catch (Exception ex)
                            {
                                throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                            }
                        }

                        subChildren.Add((workTreePath, gitDependencyLock));
                    });
                }
            }
        }

        // todo: change to re-usable worktree prefix but stateful
        public static string GetWorkTreeHeadPrefix(string branch, bool isLocked = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));

            var workTreeHeadPrefix = isLocked ? "locked-" : "";

            return $"{workTreeHeadPrefix}{HrefUtility.EscapeUrlSegment(branch)}-{branch.GetMd5HashShort()}-";
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            var dependencies = config.Dependencies.Values.Select(url =>
            {
                var (remote, branch) = HrefUtility.SplitGitHref(url);
                return (remote, branch, GitFlags.DepthOne);
            });

            dependencies = dependencies.Concat(GetThemeGitDependencies(config, locale));

            if (rootRepository != null)
            {
                dependencies = dependencies.Concat(GetLocalizationGitDependencies(rootRepository, config, locale));
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
        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetLocalizationGitDependencies(Repository repo, Config config, string locale)
        {
            if (string.IsNullOrEmpty(locale))
            {
                yield break;
            }

            if (string.Equals(locale, config.Localization.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

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
