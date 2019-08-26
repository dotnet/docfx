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
            NoCheckout = 0b0010,
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
                Progress.Update,
                maxDegreeOfParallelism: 8);

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
                var branchesToFetch = new HashSet<string>(branches);

                var repoDir = AppData.GetGitDir(remote);
                var repoPath = Path.GetFullPath(Path.Combine(repoDir, ".git"));
                var childRepos = new List<string>();

                using (InterProcessMutex.Create(remote))
                {
                    if (branchesToFetch.Count > 0)
                    {
                        try
                        {
                            using (Progress.Start($"Fetch '{remote}'"))
                            {
                                GitUtility.InitFetchBare(repoPath, remote, branchesToFetch, config);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                        }

                        using (Progress.Start($"Manage worktree for '{remote}'"))
                        {
                            AddWorkTrees(dependencyLock, group, subChildren, remote, branches, branchesToFetch, repoPath);
                        }
                    }
                }

                return subChildren.ToList();
            }
        }

        private static void AddWorkTrees(
            DependencyLockModel dependencyLock,
            IGrouping<string, (string branch, GitFlags flags)> group,
            ListBuilder<RestoreChild> subChildren,
            string remote,
            string[] branches,
            HashSet<string> branchesToFetch,
            string repoPath)
        {
            ParallelUtility.ForEach(branchesToFetch, branch =>
            {
                var nocheckout = group.Where(g => g.branch == branch).All(g => (g.flags & GitFlags.NoCheckout) != 0);
                if (nocheckout)
                {
                    return;
                }

                var gitDependencyLock = dependencyLock?.GetGitLock(remote, branch);
                var headCommit = GitUtility.RevParse(repoPath, gitDependencyLock?.Commit ?? branch);

                Log.Write($"Add worktree for `{remote}` `{headCommit}`");
                if (string.IsNullOrEmpty(headCommit))
                {
                    throw Errors.CommittishNotFound(remote, gitDependencyLock?.Commit ?? branch).ToException();
                }

                var (workTreePath, gitSlot) = RestoreGitMap.TryGetGitRestorePath(remote, branch, headCommit);
                if (workTreePath is null)
                {
                    (workTreePath, gitSlot) = RestoreGitMap.AcquireExclusiveGit(remote, branch, headCommit);
                    workTreePath = Path.GetFullPath(workTreePath).Replace('\\', '/');
                    var restored = true;

                    try
                    {
                        if (gitSlot.Restored && Directory.Exists(workTreePath))
                        {
                            // re-use existing work tree
                            // checkout to {headCommit}, no need to fetch
                            Debug.Assert(!GitUtility.IsDirty(workTreePath));
                            using (Progress.Start($"Checkout worktree {workTreePath} to {headCommit}"))
                            {
                                GitUtility.Checkout(workTreePath, headCommit);
                            }
                        }
                        else
                        {
                            // create new worktree
                            try
                            {
                                // clean existing work tree folder
                                // it may be dirty caused by last failed restore action
                                CleanWorkTreePathIfExists(workTreePath);

                                Debug.Assert(!Directory.Exists(workTreePath));
                                using (Progress.Start($"Create new worktree: {workTreePath}"))
                                {
                                    GitUtility.PruneWorkTree(repoPath);
                                    GitUtility.AddWorkTree(repoPath, headCommit, workTreePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw Errors.GitCloneFailed(remote, branches).ToException(ex);
                            }
                        }
                    }
                    catch
                    {
                        restored = false;
                        throw;
                    }
                    finally
                    {
                        RestoreGitMap.ReleaseGit(gitSlot, LockType.Exclusive, restored);
                    }
                }
                else
                {
                    Log.Write($"Worktree already exists: {workTreePath}");
                }

                Debug.Assert(workTreePath != null);
                subChildren.Add(new RestoreChild(workTreePath, remote, branch, gitDependencyLock, headCommit));
            });

            void CleanWorkTreePathIfExists(string workTreePath)
            {
                if (Directory.Exists(workTreePath))
                {
                    // https://stackoverflow.com/questions/24265481/after-directory-delete-the-directory-exists-returning-true-sometimes
                    var toDeleteDir = $"{workTreePath}-{Guid.NewGuid()}";
                    Directory.Move(workTreePath, toDeleteDir);
                    Directory.Delete(toDeleteDir, true);
                }
            }
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            foreach (var (_, dependency) in config.Dependencies)
            {
                if (dependency.Type == PackageType.Git)
                {
                    yield return (dependency.Remote, dependency.Branch, GitFlags.None);
                }
            }

            if (config.Template.Type == PackageType.Git)
            {
                var localizedTemplate = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.Localization.DefaultLocale);
                if (localizedTemplate.Type == PackageType.Git)
                {
                    yield return (localizedTemplate.Remote, localizedTemplate.Branch, GitFlags.None);
                }
            }

            foreach (var item in GetLocalizationGitDependencies(rootRepository, config, locale))
            {
                yield return item;
            }
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

            if (LocalizationUtility.TryGetFallbackRepository(repo, out var sourceRemote, out var sourceBranch, out _))
            {
                // fallback to master
                if (sourceBranch != "master" &&
                    !GitUtility.RemoteBranchExists(sourceRemote, sourceBranch, config))
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
