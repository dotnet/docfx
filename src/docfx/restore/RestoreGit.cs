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

        public static async Task<IReadOnlyDictionary<string, DependencyLockModel>> Restore(
            Config config,
            Func<string, DependencyLockModel, Task<DependencyLockModel>> restoreChild,
            string locale,
            Repository rootRepository,
            DependencyLockModel dependencyLock)
        {
            var gitVersions = new Dictionary<string, DependencyLockModel>();
            var gitDependencies =
                from git in GetGitDependencies(config, locale, rootRepository)
                group (git.branch, git.flags)
                by git.remote;

            var children = new ListBuilder<RestoreChild>();

            // restore first level children
            ParallelUtility.ForEach(
                gitDependencies,
                group =>
                {
                    foreach (var child in RestoreGitRepo(group))
                    {
                        children.Add(child);
                    }
                },
                Progress.Update);

            // fetch contribution branch
            if (rootRepository != null && LocalizationUtility.TryGetContributionBranch(rootRepository, out var contributionBranch))
            {
                GitUtility.Fetch(rootRepository.Path, rootRepository.Remote, contributionBranch, config);
            }

            // restore sub-level children
            foreach (var child in children.ToList())
            {
                var childDependencyLock = await restoreChild(child.ToRestore.path, child.ToRestore.dependencyLock);
                gitVersions.TryAdd(
                    $"{child.Restored.remote}#{child.Restored.branch}",
                    new DependencyLockModel
                    {
                        Git = childDependencyLock.Git,
                        Commit = child.Restored.commit,
                    });
            }

            return gitVersions;

            IReadOnlyList<RestoreChild> RestoreGitRepo(IGrouping<string, (string branch, GitFlags flags)> group)
            {
                var subChildren = new ListBuilder<RestoreChild>();
                var remote = group.Key;
                var branches = group.Select(g => g.branch).ToArray();
                var depthOne = group.All(g => (g.flags & GitFlags.DepthOne) != 0) && !(dependencyLock?.ContainsGitLock(remote) ?? false);
                var branchesToFetch = new HashSet<string>(branches);

                var repoDir = AppData.GetGitDir(remote);
                var repoPath = Path.GetFullPath(Path.Combine(repoDir, ".git"));
                var childRepos = new List<string>();

                ProcessUtility.RunInsideMutex(
                    remote,
                    () =>
                    {
                        if (branchesToFetch.Count > 0)
                        {
                            try
                            {
                                GitUtility.CloneOrUpdateBare(repoPath, remote, branchesToFetch, depthOne, config);
                            }
                            catch (Exception ex)
                            {
                                throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                            }
                            AddWorkTrees();
                        }
                    });

                return subChildren.ToList();

                void AddWorkTrees()
                {
                    var existingWorkTreeFolders = new ConcurrentHashSet<string>(Directory.EnumerateDirectories(repoDir));
                    ParallelUtility.ForEach(branchesToFetch, branch =>
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

                        var (workTreePath, gitSlot) = RestoreMap.TryGetGitRestorePath(remote, branch, headCommit);
                        if (workTreePath is null)
                        {
                            (workTreePath, gitSlot) = RestoreMap.AcquireExclusiveGit(remote, branch, headCommit);
                            workTreePath = Path.GetFullPath(workTreePath).Replace('\\', '/');
                            var restored = true;

                            try
                            {
                                if (existingWorkTreeFolders.TryAdd(workTreePath))
                                {
                                    // create new worktree
                                    try
                                    {
                                        GitUtility.AddWorkTree(repoPath, headCommit, workTreePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                                    }
                                }
                                else
                                {
                                    // worktree already exists
                                    // checkout to {headCommit}, no need to fetch
                                    GitUtility.Checkout(workTreePath, headCommit);
                                }
                            }
                            catch
                            {
                                restored = false;
                                throw;
                            }
                            finally
                            {
                                RestoreMap.ReleaseGit(gitSlot, LockType.Exclusive, restored);
                            }
                        }

                        Debug.Assert(workTreePath != null);
                        subChildren.Add(new RestoreChild(workTreePath, remote, branch, gitDependencyLock, headCommit));
                    });
                }
            }
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            var dependencies = config.Dependencies.Values.Select(url =>
            {
                var (remote, branch, _) = UrlUtility.SplitGitUrl(url);
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

            if (repo is null || string.IsNullOrEmpty(repo.Remote))
            {
                yield break;
            }

            if (LocalizationUtility.TryGetSourceRepository(repo, out var sourceRemote, out var sourceBranch, out _))
            {
                // fallback to master
                if (sourceBranch != "master" &&
                    !GitUtility.RemoteBranchExists(sourceRemote, sourceBranch))
                {
                    sourceBranch = "master";
                }

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

        private class RestoreChild
        {
            public (string path, DependencyLockModel dependencyLock) ToRestore { get; private set; }

            public (string remote, string branch, string commit) Restored { get; private set; }

            public RestoreChild(string path, string remote, string branch, DependencyLockModel dependencyLock, string commit)
            {
                Debug.Assert(!string.IsNullOrEmpty(path));
                Debug.Assert(!string.IsNullOrEmpty(remote));
                Debug.Assert(!string.IsNullOrEmpty(branch));
                Debug.Assert(!string.IsNullOrEmpty(commit));

                Restored = (remote, branch, commit);
                ToRestore = (path, dependencyLock);
            }
        }
    }
}
