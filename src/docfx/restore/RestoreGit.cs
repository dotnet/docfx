// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;

namespace Microsoft.Docs.Build
{
    internal static class RestoreGit
    {
        internal static IReadOnlyList<RestoreGitResult> Restore(
            Config config,
            string locale,
            Repository repository,
            Dictionary<PackageUrl, DependencyGitLock> dependencyLock)
        {
            var gitDependencies =
                from git in GetGitDependencies(config, locale, repository)
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
            if (repository != null && LocalizationUtility.TryGetContributionBranch(repository, out var contributionBranch))
            {
                GitUtility.Fetch(repository.Path, repository.Remote, contributionBranch, config);
            }

            return results.ToList();
        }

        internal static IReadOnlyList<RestoreGitResult> RestoreGitRepo(
            Config config,
            string remote,
            List<(string branch, RestoreGitFlags flags)> branches,
            Dictionary<PackageUrl, DependencyGitLock> dependencyLock)
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
                        return AddWorkTrees(repoPath, remote, branches, branchesToFetch, dependencyLock);
                    }
                }
            }

            return new List<RestoreGitResult>();
        }

        private static IReadOnlyList<RestoreGitResult> AddWorkTrees(
            string repoPath,
            string remote,
            List<(string branch, RestoreGitFlags flags)> branches,
            HashSet<string> branchesToFetch,
            Dictionary<PackageUrl, DependencyGitLock> dependencyLock)
        {
            var results = new ListBuilder<RestoreGitResult>();
            ParallelUtility.ForEach(branchesToFetch, branch =>
            {
                var gitLock = dependencyLock.GetGitLock(new PackageUrl(remote, branch));
                var headCommit = GitUtility.RevParse(repoPath, gitLock?.Commit ?? branch);

                Log.Write($"Add worktree for `{remote}` `{headCommit}`");
                if (string.IsNullOrEmpty(headCommit))
                {
                    throw Errors.CommittishNotFound(remote, gitLock?.Commit ?? branch).ToException();
                }

                var nocheckout = branches.Where(g => g.branch == branch).All(g => (g.flags & RestoreGitFlags.NoCheckout) != 0);
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

        private static IEnumerable<(string remote, string branch, RestoreGitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            foreach (var (_, dependency) in config.Dependencies)
            {
                var dependencyUrl = dependency.Url;
                if (dependencyUrl.Type == PackageType.Git)
                {
                    yield return (dependencyUrl.Remote, dependencyUrl.Branch, RestoreGitFlags.None);
                }
            }

            if (config.Template.Type == PackageType.Git)
            {
                var localizedTemplate = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.Localization.DefaultLocale);
                if (localizedTemplate.Type == PackageType.Git)
                {
                    yield return (localizedTemplate.Remote, localizedTemplate.Branch, RestoreGitFlags.None);
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
        private static IEnumerable<(string remote, string branch, RestoreGitFlags flags)> GetLocalizationGitDependencies(Repository repo, Config config, string locale)
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

            yield return (remote, branch, RestoreGitFlags.None);

            if (config.Localization.Bilingual && LocalizationUtility.TryGetContributionBranch(branch, out var contributionBranch))
            {
                // Bilingual repos also depend on non bilingual branch for commit history
                yield return (remote, contributionBranch, RestoreGitFlags.NoCheckout);
            }
        }
    }
}
