// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class RestoreMap
    {
        private IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> _acquiredGits;

        public RestoreMap(IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> acquiredGits)
        {
            _acquiredGits = acquiredGits ?? new Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)>();
        }

        /// <summary>
        /// Acquire restored git repository path, dependencyLock and git slot with url and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public (string path, DependencyLockModel subDependencyLock) GetGitRestorePath(string url, DependencyLockModel dependencyLock)
        {
            var (remote, branch, _) = HrefUtility.SplitGitHref(url);
            return GetGitRestorePath(remote, branch, dependencyLock);
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public (string path, DependencyLockModel subDependencyLock) GetGitRestorePath(string remote, string branch, DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var gitVersion = dependencyLock.GetGitLock(remote, branch);

            if (gitVersion is null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            if (!_acquiredGits.TryGetValue((remote, branch, gitVersion.Commit), out var gitInfo))
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            if (string.IsNullOrEmpty(gitInfo.path) || gitInfo.git is null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            var path = Path.Combine(AppData.GetGitDir(remote), gitInfo.path);
            Debug.Assert(Directory.Exists(path));

            return (path, gitVersion);
        }

        public bool Release()
        {
            var released = true;
            foreach (var (k, v) in _acquiredGits)
            {
                released &= ReleaseGit(v.git, LockType.Shared);
            }

            Debug.Assert(released);

            return released;
        }

        public static (string localPath, string content, string etag) GetRestoredFileContent(Docset docset, string url)
        {
            return GetRestoredFileContent(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static (string localPath, string content, string etag) GetRestoredFileContent(string docsetPath, string url, string fallbackDocset = null)
        {
            var fromUrl = HrefUtility.IsHttpHref(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return (fullPath, File.ReadAllText(fullPath), null);
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    return GetRestoredFileContent(fallbackDocset, url);
                }

                throw Errors.FileNotFound(docsetPath, new SourceInfo<string>(url)).ToException();
            }

            var (content, etag) = TryGetRestoredFileContent(url);
            if (string.IsNullOrEmpty(content))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (null, content, etag);
        }

        public static (string content, string etag) TryGetRestoredFileContent(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            var filePath = RestoreFile.GetRestoreContentPath(url);
            var etagPath = RestoreFile.GetRestoreEtagPath(url);
            string etag = null;
            string content = null;

            ProcessUtility.RunInsideMutex(filePath, () =>
            {
                content = GetFileContentIfExists(filePath);
                etag = GetFileContentIfExists(etagPath);

                string GetFileContentIfExists(string file)
                {
                    if (File.Exists(file))
                    {
                        return File.ReadAllText(file);
                    }

                    return null;
                }
            });

            return (content, etag);
        }

        /// <summary>
        /// Acquired all shared git based on dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static RestoreMap
            Create(
            DependencyLockModel dependencyLock,
            Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> acquired = null)
        {
            Debug.Assert(dependencyLock != null);

            RestoreMap gitPool = null;
            var root = acquired is null;
            acquired = acquired ?? new Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)>();

            var successed = true;
            try
            {
                foreach (var gitVersion in dependencyLock.Git)
                {
                    var (remote, branch, _) = HrefUtility.SplitGitHref(gitVersion.Key);
                    if (!acquired.ContainsKey((remote, branch, gitVersion.Value.Commit/*commit*/)))
                    {
                        var (path, git) = AcquireGit(remote, branch, gitVersion.Value.Commit, LockType.Shared);
                        acquired[(remote, branch, gitVersion.Value.Commit/*commit*/)] = (path, git);
                    }

                    Create(gitVersion.Value, acquired);
                }

                gitPool = new RestoreMap(acquired);
                return gitPool;
            }
            catch
            {
                successed = false;
                throw;
            }
            finally
            {
                if (!successed && root)
                {
                    foreach (var (k, v) in acquired)
                    {
                        ReleaseGit(v.git, LockType.Shared, false);
                    }
                }
            }
        }

        /// <summary>
        /// Try get git dependency repository path and git slot with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static (string path, DependencyGit git) TryGetGitRestorePath(string remote, string branch, string commit)
        {
            var restoreDir = AppData.GetGitDir(remote);

            var (path, slot) = DependencySlotPool<DependencyGit>.TryGetSlot(
                remote,
                gits =>
                {
                    var filteredGits = gits.Where(i => i.Branch == branch);

                    if (!string.IsNullOrEmpty(commit))
                    {
                        // found commit matched slot
                        filteredGits = filteredGits.Where(i => i.Commit == commit);
                    }

                    var commits = Array.Empty<string>();
                    if (filteredGits.Count() > 1)
                    {
                        commits = GitUtility.GetCommits(restoreDir, branch, 1000/*top 1000 should be enough for comparing*/);
                    }

                    return filteredGits.OrderBy(g => Array.IndexOf(commits, g.Commit)).ThenByDescending(g => g.LastAccessDate).ToList();
                });

            if (!string.IsNullOrEmpty(path))
                path = Path.Combine(AppData.GetGitDir(remote), path);

            return !Directory.Exists(path) ? default : (path, slot);
        }

        public static (string path, DependencyGit git) AcquireExclusiveGit(string remote, string branch, string commit)
        {
            var (path, git) = AcquireGit(remote, branch, commit, LockType.Exclusive);

            Debug.Assert(path != null && git != null);
            path = Path.Combine(AppData.GetGitDir(remote), path);

            return (path, git);
        }

        public static bool ReleaseGit(DependencyGit git, LockType lockType, bool successed = true)
            => DependencySlotPool<DependencyGit>.ReleaseSlot(git, lockType, successed);

        private static (string path, DependencyGit git) AcquireGit(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            return DependencySlotPool<DependencyGit>.AcquireSlot(
                remote,
                type,
                slot =>
                {
                    // update branch and commit info to new rented slot
                    slot.Commit = commit;
                    slot.Branch = branch;
                    return slot;
                },
                existingSlot => existingSlot.Branch == branch && existingSlot.Commit == commit);
        }
    }
}
