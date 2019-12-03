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
            Repository repository,
            DependencyLockProvider dependencyLockProvider)
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
                    results.AddRange(RestoreGitRepo(config, group.Key, group.ToList(), dependencyLockProvider));
                },
                Progress.Update,
                maxDegreeOfParallelism: 8);

            // fetch contribution branch
            if (repository != null && LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch))
            {
                GitUtility.Fetch(repository.Path, repository.Remote, contributionBranch, config);
            }

            return results.ToList();
        }

        internal static IReadOnlyList<RestoreGitResult> RestoreGitRepo(
            Config config,
            string remote,
            List<(string branch, RestoreGitFlags flags)> branches,
            DependencyLockProvider dependencyLockProvider)
        {
            var branchesToFetch = new HashSet<string>(branches.Select(b => b.branch));
            var repoDir = AppData.GetGitDir(remote);
            var repoPath = Path.GetFullPath(Path.Combine(repoDir, ".git"));

            using (InterProcessReaderWriterLock.CreateWriterLock(remote))
            {
                if (branchesToFetch.Count > 0)
                {
                    try
                    {
                        using (PerfScope.Start($"Fetch '{remote}'"))
                        {
                            GitUtility.InitFetchBare(repoPath, remote, branchesToFetch, config);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw Errors.GitCloneFailed(remote, branches.Select(b => b.branch)).ToException(ex);
                    }

                    using (PerfScope.Start($"Manage worktree for '{remote}'"))
                    {
                        return AddWorkTrees(repoPath, remote, branches, dependencyLockProvider);
                    }
                }
            }

            return new List<RestoreGitResult>();
        }

        private static IReadOnlyList<RestoreGitResult> AddWorkTrees(
            string repoPath,
            string remote,
            List<(string branch, RestoreGitFlags flags)> branches,
            DependencyLockProvider dependencyLockProvider)
        {
            var branchesToFetch = new HashSet<string>(branches.Select(b => b.branch));
            var results = new ListBuilder<RestoreGitResult>();
            ParallelUtility.ForEach(branchesToFetch, branch =>
            {
                var gitLock = dependencyLockProvider?.GetGitLock(remote, branch);
                var headCommit = GitUtility.RevParse(repoPath, gitLock?.Commit ?? branch);
                if (string.IsNullOrEmpty(headCommit))
                {
                    throw Errors.CommittishNotFound(remote, gitLock?.Commit ?? branch).ToException();
                }

                var nocheckout = branches.Where(g => g.branch == branch).All(g => (g.flags & RestoreGitFlags.NoCheckout) != 0);
                if (nocheckout)
                {
                    results.Add(new RestoreGitResult(repoPath, remote, branch, headCommit));
                    return;
                }

                // always share the same worktree
                // todo: remove worktree once we can get files from git for Template and Localization/Fallback repo.
                Log.Write($"Add worktree for `{remote}` `{headCommit}`");
                var workTreePath = Path.Combine(repoPath.Substring(0, repoPath.Length - ".git".Length), "1");
                if (!Directory.Exists(workTreePath))
                {
                    using (PerfScope.Start($"Create new worktree: {workTreePath}"))
                    {
                        GitUtility.PruneWorkTree(repoPath);
                        GitUtility.AddWorkTree(repoPath, headCommit, workTreePath);
                    }
                }
                else
                {
                    // re-use existing work tree
                    // checkout to {headCommit}, no need to fetch
                    Debug.Assert(!GitUtility.IsDirty(workTreePath));
                    if (GitUtility.RevParse(workTreePath) != headCommit)
                    {
                        using (PerfScope.Start($"Checkout worktree {workTreePath} to {headCommit}"))
                        {
                            GitUtility.Checkout(workTreePath, headCommit);
                        }
                    }
                    else
                    {
                        Log.Write($"Worktree already exists: {workTreePath}");
                    }
                }

                Debug.Assert(workTreePath != null);
                results.Add(new RestoreGitResult(workTreePath, remote, branch, headCommit));
            });

            return results.ToList();
        }

        private static IEnumerable<(string remote, string branch, RestoreGitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            foreach (var (_, url) in config.Dependencies)
            {
                if (url.Type == PackageType.Git)
                {
                    yield return (url.Url, url.Branch, RestoreGitFlags.NoCheckout);
                }
            }

            if (config.Template.Type == PackageType.Git)
            {
                var localizedTemplate = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.Localization.DefaultLocale);
                if (localizedTemplate.Type == PackageType.Git)
                {
                    yield return (localizedTemplate.Url, localizedTemplate.Branch, RestoreGitFlags.None);
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
