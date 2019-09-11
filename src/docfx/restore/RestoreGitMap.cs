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
        private readonly string _docsetPath;
        private readonly DependencyLockProvider _dependencyLockProvider;
        private readonly IReadOnlyDictionary<(string url, string branch, string commit), (string path, DependencyGit git)> _acquiredGits;

        private RestoreGitMap(
            string docsetPath,
            DependencyLockProvider dependencyLockProvider,
            IReadOnlyDictionary<(string url, string branch, string commit), (string path, DependencyGit git)> acquiredGits)
        {
            _docsetPath = docsetPath;
            _dependencyLockProvider = dependencyLockProvider;
            _acquiredGits = acquiredGits;
        }

        public string GetGitRestorePath(PackageUrl packageUrl)
        {
            switch (packageUrl.Type)
            {
                case PackageType.Folder:
                    var fullPath = Path.Combine(_docsetPath, packageUrl.Path);
                    if (Directory.Exists(fullPath))
                    {
                        return fullPath;
                    }

                    // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                    // TODO: populate source info
                    throw Errors.FileNotFound(new SourceInfo<string>(packageUrl.Path)).ToException();

                case PackageType.Git:
                    var gitLock = _dependencyLockProvider.GetGitLock(packageUrl.Url, packageUrl.Branch);

                    if (gitLock is null)
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    if (!_acquiredGits.TryGetValue((packageUrl.Url, packageUrl.Branch, gitLock.Commit), out var gitInfo))
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    if (string.IsNullOrEmpty(gitInfo.path) || gitInfo.git is null)
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    var path = Path.Combine(AppData.GetGitDir(packageUrl.Url), gitInfo.path);
                    if (!Directory.Exists(path))
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    return path;

                default:
                    throw new NotSupportedException($"Unknown package url: '{packageUrl}'");
            }
        }

        public bool IsBranchRestored(string remote, string branch)
        {
            var packageUrl = new PackageUrl(remote, branch);
            var gitLock = _dependencyLockProvider.GetGitLock(remote, branch);

            if (gitLock is null)
            {
                return false;
            }

            if (!_acquiredGits.TryGetValue((packageUrl.Url, packageUrl.Branch, gitLock.Commit), out var gitInfo))
            {
                return false;
            }

            if (string.IsNullOrEmpty(gitInfo.path) || gitInfo.git is null)
            {
                return false;
            }

            return true;
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
        public static RestoreGitMap Create(string docsetPath, Config config, string locale)
        {
            var acquired = new Dictionary<(string url, string branch, string commit), (string path, DependencyGit git)>();

            try
            {
                var dependencyLockPath = string.IsNullOrEmpty(config.DependencyLock)
                    ? new SourceInfo<string>(AppData.GetDependencyLockFile(docsetPath, locale)) : config.DependencyLock;

                var dependencyLockProvider = DependencyLockProvider.Create(docsetPath, dependencyLockPath);

                foreach (var (url, branch, gitLock) in dependencyLockProvider.ListAll())
                {
                    if (!acquired.ContainsKey((url, branch, gitLock.Commit)))
                    {
                        var (path, git) = AcquireGit(url, branch, gitLock.Commit, LockType.Shared);
                        acquired[(url, branch, gitLock.Commit)] = (path, git);
                    }
                }

                return new RestoreGitMap(docsetPath, dependencyLockProvider, acquired);
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
