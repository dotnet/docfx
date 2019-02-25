// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class DependencyGitPool
    {
        /// <summary>
        /// Try get git dependency repository path and git slot with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static Task<(string path, DependencyGit git)> TryGetGit(string remote, string branch, string commit)
        {
            return DependencySlotPool.TryGetSlot<DependencyGit>(remote, gits =>
            {
                var filteredGits = gits.Where(i => i.Branch == branch);
                if (!string.IsNullOrEmpty(commit))
                {
                    // found commit matched slot
                    filteredGits = filteredGits.Where(i => i.Commit == commit);
                }

                return filteredGits.ToList();
            });
        }

        /// <summary>
        /// Acquire restored git repository path, dependencyLock and git slot with url and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static Task<(string path, DependencyLockModel subDependencyLock, DependencyGit git)> AcquireSharedGit(string url, DependencyLockModel dependencyLock)
        {
            var (remote, branch, _) = HrefUtility.SplitGitHref(url);
            return AcquireSharedGit(remote, branch, dependencyLock);
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static async Task<(string path, DependencyLockModel subDependencyLock, DependencyGit git)> AcquireSharedGit(string remote, string branch, DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var gitVersion = dependencyLock.GetGitLock(remote, branch);

            if (gitVersion == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            var (path, git) = await AcquireGit(remote, branch, gitVersion.Commit, LockType.Shared);

            if (string.IsNullOrEmpty(path) || git == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            path = Path.Combine(AppData.GetGitDir(remote), path);
            Debug.Assert(Directory.Exists(path));

            return (path, gitVersion, git);
        }

        public static Task<(string path, DependencyGit git)> AcquireExclusiveGit(string remote, string branch, string commit)
            => AcquireGit(remote, branch, commit, LockType.Exclusive);

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
