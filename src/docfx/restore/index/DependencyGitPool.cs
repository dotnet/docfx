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
        private Dictionary<(string remote, string branch, string commit, LockType lockType), (string path, DependencyGit git, bool released)> _acquiredGits = new Dictionary<(string remote, string branch, string commit, LockType lockType), (string path, DependencyGit git, bool released)>();

        /// <summary>
        /// Acquired all shared git based on dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public async Task<List<DependencyGit>> AcquireSharedGits(DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var successed = true;
            var gits = new List<DependencyGit>();
            try
            {
                foreach (var gitVersion in dependencyLock.Git)
                {
                    var (remote, branch, _) = HrefUtility.SplitGitHref(gitVersion.Key);
                    if (!_acquiredGits.ContainsKey((remote, branch, gitVersion.Value.Commit/*commit*/, LockType.Shared)))
                    {
                        var (path, git) = await AcquireGit(remote, branch, gitVersion.Value.Commit, LockType.Shared);
                        _acquiredGits[(remote, branch, gitVersion.Value.Commit/*commit*/, LockType.Shared)] = (path, git, false);
                        gits.Add(git);
                    }

                    gits.AddRange(await AcquireSharedGits(gitVersion.Value));
                }
            }
            catch
            {
                successed = false;
                throw;
            }
            finally
            {
                if (!successed)
                {
                    foreach (var git in gits)
                    {
                        await DependencySlotPool.ReleaseSlot(git, LockType.Shared, false);
                    }
                }
            }

            return gits;
        }

        public async Task<bool> ReleaseSharedGits(DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var released = true;
            foreach (var gitVersion in dependencyLock.Git)
            {
                var (remote, branch, _) = HrefUtility.SplitGitHref(gitVersion.Key);
                var contained = _acquiredGits.TryGetValue((remote, branch, gitVersion.Value.Commit, LockType.Shared), out var gitSlot);
                Debug.Assert(contained);
                if (!gitSlot.released)
                {
                    released &= await DependencySlotPool.ReleaseSlot(gitSlot.git, LockType.Shared);
                    _acquiredGits[(remote, branch, gitVersion.Value.Commit, LockType.Shared)] = (gitSlot.path, gitSlot.git, true);
                }
                released &= await ReleaseSharedGits(gitVersion.Value);
            }

            Debug.Assert(released);

            return released;
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

            if (!_acquiredGits.TryGetValue((remote, branch, gitVersion.Commit, LockType.Shared), out var gitInfo))
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

            return DependencySlotPool.AcquireSlots<DependencyGit>(
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
