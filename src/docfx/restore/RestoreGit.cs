// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class RestoreGit
    {
        internal static IReadOnlyList<RestoreGitResult> Restore(
            Config config,
            string locale,
            Repository rootRepository,
            Dictionary<string, string> dependencyLock)
        {
            var gitDependencies =
                from git in GetGitDependencies(config, locale, rootRepository)
                group (git.branch, git.flags)
                by git.remote;

            var results = new ListBuilder<RestoreGitResult>();

            ParallelUtility.ForEach(
                gitDependencies,
                group =>
                {
                    results.AddRange(RestoreGitRepo(config, group.Key, group.ToList(), dependencyLock));
                },
                Progress.Update,
                maxDegreeOfParallelism: 8);

            // fetch contribution branch
            if (rootRepository != null && LocalizationUtility.TryGetContributionBranch(rootRepository, out var contributionBranch))
            {
                GitUtility.Fetch(rootRepository.Path, rootRepository.Remote, contributionBranch, config);
            }

            return results.ToList();
        }

        internal static IReadOnlyList<RestoreGitResult> RestoreGitRepo(Config config, string remote, List<(string branch, GitFlags flags)> branches, Dictionary<string, string> dependencyLock)
        {
            var branchesToFetch = new HashSet<string>(branches.Select(b => b.branch));
            var repoDir = AppData.GetGitDir(remote);
            var repoPath = Path.GetFullPath(Path.Combine(repoDir, ".git"));

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
                        throw Errors.GitCloneFailed(remote, branches.Select(b => b.branch)).ToException(ex);
                    }

                    using (Progress.Start($"Manage worktree for '{remote}'"))
                    {
                        return AddWorkTrees(dependencyLock, remote, branches, branchesToFetch, repoPath);
                    }
                }
            }

            return new List<RestoreGitResult>();
        }

        private static IReadOnlyList<RestoreGitResult> AddWorkTrees(
            Dictionary<string, string> dependencyLock,
            string remote,
            List<(string branch, GitFlags flags)> branches,
            HashSet<string> branchesToFetch,
            string repoPath)
        {
            var results = new ListBuilder<RestoreGitResult>();
            ParallelUtility.ForEach(branchesToFetch, branch =>
            {
                var gitLockCommit = DependencyLockProvider.GetGitLock(dependencyLock, remote, branch);
                var headCommit = GitUtility.RevParse(repoPath, gitLockCommit ?? branch);

                Log.Write($"Add worktree for `{remote}` `{headCommit}`");
                if (string.IsNullOrEmpty(headCommit))
                {
                    throw Errors.CommittishNotFound(remote, gitLockCommit ?? branch).ToException();
                }

                var nocheckout = branches.Where(g => g.branch == branch).All(g => (g.flags & GitFlags.NoCheckout) != 0);
                if (nocheckout)
                {
                    return;
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
                                throw Errors.GitCloneFailed(remote, branches.Select(b => b.branch)).ToException(ex);
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
                results.Add(new RestoreGitResult(workTreePath, remote, branch, headCommit));
            });

            return results.ToList();

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

            if (LocalizationUtility.TryGetFallbackRepository(repo, out _, out _, out _))
            {
                yield break;
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
