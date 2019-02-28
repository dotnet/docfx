// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class DependencyGitPool
    {
        private IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> _acquiredGits;

        public DependencyGitPool(IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> acquiredGits)
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

            if (gitVersion == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            if (!_acquiredGits.TryGetValue((remote, branch, gitVersion.Commit), out var gitInfo))
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            if (string.IsNullOrEmpty(gitInfo.path) || gitInfo.git == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            var path = Path.Combine(AppData.GetGitDir(remote), gitInfo.path);
            Debug.Assert(Directory.Exists(path));

            return (path, gitVersion);
        }

        public async Task<bool> Release()
        {
            var released = true;
            foreach (var (k, v) in _acquiredGits)
            {
                released &= await DependencySlotPool.ReleaseSlot(v.git, LockType.Shared);
            }

            Debug.Assert(released);

            return released;
        }

        /// <summary>
        /// Acquired all shared git based on dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static async Task<DependencyGitPool>
            AcquireGitPool(
            DependencyLockModel dependencyLock,
            Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> acquired = null)
        {
            Debug.Assert(dependencyLock != null);

            var root = acquired == null;
            acquired = acquired ?? new Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)>();

            var successed = true;
            try
            {
                foreach (var gitVersion in dependencyLock.Git)
                {
                    var (remote, branch, _) = HrefUtility.SplitGitHref(gitVersion.Key);
                    if (!acquired.ContainsKey((remote, branch, gitVersion.Value.Commit/*commit*/)))
                    {
                        var (path, git) = await AcquireGit(remote, branch, gitVersion.Value.Commit, LockType.Shared);
                        acquired[(remote, branch, gitVersion.Value.Commit/*commit*/)] = (path, git);
                    }

                    await AcquireGitPool(gitVersion.Value, acquired);
                }
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
                        await DependencySlotPool.ReleaseSlot(v.git, LockType.Shared, false);
                    }
                }
            }

            return new DependencyGitPool(acquired);
        }

        /// <summary>
        /// Try get git dependency repository path and git slot with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static async Task<(string path, DependencyGit git)> TryGetGitRestorePath(string remote, string branch, string commit)
        {
            var (path, slot) = await DependencySlotPool.TryGetSlot<DependencyGit>(remote, gits =>
            {
                var filteredGits = gits.Where(i => i.Branch == branch);
                if (!string.IsNullOrEmpty(commit))
                {
                    // found commit matched slot
                    filteredGits = filteredGits.Where(i => i.Commit == commit);
                }

                return filteredGits.ToList();
            });

            if (!string.IsNullOrEmpty(path))
                path = Path.Combine(AppData.GetGitDir(remote), path);

            return !Directory.Exists(path) ? default : (path, slot);
        }

        public static async Task<(string path, DependencyGit git)> AcquireExclusiveGit(string remote, string branch, string commit)
        {
            var (path, git) = await AcquireGit(remote, branch, commit, LockType.Exclusive);

            Debug.Assert(path != null && git != null);
            path = Path.Combine(AppData.GetGitDir(remote), path);

            return (path, git);
        }

        private static Task<(string path, DependencyGit git)> AcquireGit(string remote, string branch, string commit, LockType type)
        {
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            return DependencySlotPool.AcquireSlot<DependencyGit>(
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
