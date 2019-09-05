// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RestoreGitMap : IDisposable
    {
        private readonly IReadOnlyDictionary<(PackageUrl packageUrl, string commit), (string path, DependencyGit git)> _acquiredGits;

        public Dictionary<PackageUrl, DependencyGitLock> DependencyGitLock { get; private set; }

        public RestoreGitMap(IReadOnlyDictionary<(PackageUrl packageUrl, string commit), (string path, DependencyGit git)> acquiredGits = null)
        {
            _acquiredGits = acquiredGits ?? new Dictionary<(PackageUrl packageUrl, string commit), (string path, DependencyGit git)>();
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public string GetGitRestorePath(PackageUrl url, string docsetPath)
        {
            switch (url.Type)
            {
                case PackageType.Folder:
                    var fullPath = Path.Combine(docsetPath, url.Path);
                    if (Directory.Exists(fullPath))
                    {
                        return fullPath;
                    }

                    // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                    // TODO: populate source info
                    throw Errors.FileNotFound(new SourceInfo<string>(url.Path)).ToException();

                case PackageType.Git:
                    return GetGitRestorePath(url);

                default:
                    throw new NotSupportedException($"Unknown package url: '{url}'");
            }
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public string GetGitRestorePath(PackageUrl packageUrl)
        {
            var gitLock = DependencyGitLock.GetGitLock(packageUrl);

            if (gitLock is null)
            {
                throw Errors.NeedRestore($"{packageUrl}").ToException();
            }

            if (!_acquiredGits.TryGetValue((packageUrl, gitLock.Commit), out var gitInfo))
            {
                throw Errors.NeedRestore($"{packageUrl}").ToException();
            }

            if (string.IsNullOrEmpty(gitInfo.path) || gitInfo.git is null)
            {
                throw Errors.NeedRestore($"{packageUrl}").ToException();
            }

            var path = Path.Combine(AppData.GetGitDir(packageUrl.Remote), gitInfo.path);
            Debug.Assert(Directory.Exists(path));

            return path;
        }

        public void Dispose()
        {
            var released = true;
            foreach (var (k, v) in _acquiredGits)
            {
                released &= ReleaseGit(v.git, LockType.Shared);
            }

            Debug.Assert(released);
        }

        /// <summary>
        /// Acquired all shared git based on dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static RestoreGitMap Create(Dictionary<PackageUrl, DependencyGitLock> dependencyLock)
        {
            var acquired = new Dictionary<(PackageUrl packageUrl, string commit), (string path, DependencyGit git)>();

            try
            {
                Debug.Assert(dependencyLock != null);

                foreach (var (packageUrl, gitLock) in dependencyLock)
                {
                    if (!acquired.ContainsKey((packageUrl, gitLock.Commit)))
                    {
                        var (path, git) = AcquireGit(packageUrl.Remote, packageUrl.Branch, gitLock.Commit, LockType.Shared);
                        acquired[(packageUrl, gitLock.Commit)] = (path, git);
                    }
                }

                return new RestoreGitMap(acquired)
                {
                    DependencyGitLock = dependencyLock,
                };
            }
            catch
            {
                foreach (var (k, v) in acquired)
                {
                    ReleaseGit(v.git, LockType.Shared, false);
                }
                throw;
            }
        }

        /// <summary>
        /// Try get git dependency repository path and git slot with remote, branch and dependency version(commit).
        /// If the dependency version is null, get the latest one(order by last write time).
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static (string path, DependencyGit git) TryGetGitRestorePath(string remote, string branch, string commit)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));
            Debug.Assert(!string.IsNullOrEmpty(branch));
            Debug.Assert(!string.IsNullOrEmpty(commit));

            var restoreDir = AppData.GetGitDir(remote);

            var (path, slot) = DependencySlotPool<DependencyGit>.TryGetSlot(remote, gits => gits.Where(i => i.Branch == branch && i.Commit == commit).OrderByDescending(g => g.LastAccessDate).ToList());

            if (!string.IsNullOrEmpty(path))
                path = Path.Combine(restoreDir, path);

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
