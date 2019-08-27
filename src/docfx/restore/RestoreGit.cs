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

        public static async Task<IReadOnlyDictionary<string, DependencyGitLock>> Restore(
            Config config,
            Func<string, DependencyGitLock, Task<DependencyGitLock>> restoreChild,
            string locale,
            Repository rootRepository,
            DependencyGitLock dependencyLock)
        {
            var gitVersions = new Dictionary<string, DependencyGitLock>();
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
                // todo: remove restoring for sub children
                var childDependencyLock = await restoreChild(child.ToRestore.path, child.ToRestore.dependencyLock);
                gitVersions.TryAdd(
                    $"{child.Restored.remote}#{child.Restored.branch}",
                    new DependencyGitLock
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

                using (new SharedAndExclusiveLock(remote, shared: false))
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
                            AddWorkTrees(dependencyLock, group, subChildren, remote, branchesToFetch, repoPath);
                        }
                    }
                }

                return subChildren.ToList();
            }
        }

        private static void AddWorkTrees(
            DependencyGitLock dependencyLock,
            IGrouping<string, (string branch, GitFlags flags)> group,
            ListBuilder<RestoreChild> subChildren,
            string remote,
            HashSet<string> branchesToFetch,
            string repoPath)
        {
            ParallelUtility.ForEach(branchesToFetch, branch =>
            {
                var gitDependencyLock = dependencyLock?.GetGitLock(remote, branch);
                var headCommit = GitUtility.RevParse(repoPath, gitDependencyLock?.Commit ?? branch);

                var nocheckout = group.Where(g => g.branch == branch).All(g => (g.flags & GitFlags.NoCheckout) != 0);
                if (nocheckout)
                {
                    subChildren.Add(new RestoreChild(repoPath, remote, branch, gitDependencyLock, headCommit));
                    return;
                }

                Log.Write($"Add worktree for `{remote}` `{headCommit}`");
                if (string.IsNullOrEmpty(headCommit))
                {
                    throw Errors.CommittishNotFound(remote, gitDependencyLock?.Commit ?? branch).ToException();
                }

                // always share the same worktree
                // todo: remove worktree once we can get files from git for Template and Localization/Fallback repo.
                var workTreePath = Path.Combine(repoPath.Substring(0, ".git".Length), "1");
                if (!Directory.Exists(workTreePath))
                {
                    using (Progress.Start($"Create new worktree: {workTreePath}"))
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
                        using (Progress.Start($"Checkout worktree {workTreePath} to {headCommit}"))
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
                subChildren.Add(new RestoreChild(workTreePath, remote, branch, gitDependencyLock, headCommit));
            });
        }

        private static IEnumerable<(string remote, string branch, GitFlags flags)> GetGitDependencies(Config config, string locale, Repository rootRepository)
        {
            foreach (var (_, url) in config.Dependencies)
            {
                var (remote, branch, _) = UrlUtility.SplitGitUrl(url);
                if (UrlUtility.IsHttp(url))
                {
                    yield return (remote, branch, GitFlags.NoCheckout);
                }
            }

            if (UrlUtility.IsHttp(config.Template))
            {
                var (remote, branch) = LocalizationUtility.GetLocalizedTheme(config.Template, locale, config.Localization.DefaultLocale);

                yield return (remote, branch, GitFlags.None);
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
            public (string path, DependencyGitLock dependencyLock) ToRestore { get; private set; }

            public (string remote, string branch, string commit) Restored { get; private set; }

            public RestoreChild(string path, string remote, string branch, DependencyGitLock dependencyLock, string commit)
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
