// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RestoreMap
    {
        private readonly IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> _acquiredGits;

        public DependencyLockModel DependencyLock { get; private set; }

        public RestoreMap(IReadOnlyDictionary<(string remote, string branch, string commit), (string path, DependencyGit git)> acquiredGits = null)
        {
            _acquiredGits = acquiredGits ?? new Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)>();
        }

        /// <summary>
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public (string path, RestoreMap subRestoreMap) GetGitRestorePath(string remote, string branch, string docsetPath)
        {
            if (!UrlUtility.IsHttp(remote))
            {
                var fullPath = Path.Combine(docsetPath, remote);
                if (Directory.Exists(fullPath))
                {
                    return (fullPath, new RestoreMap(_acquiredGits));
                }

                // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                // TODO: populate source info
                throw Errors.FileNotFound(new SourceInfo<string>(remote)).ToException();
            }

            var gitVersion = DependencyLock.GetGitLock(remote, branch);

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

            return (path, new RestoreMap(_acquiredGits) { DependencyLock = gitVersion });
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

        public static (string localPath, string content, string etag) GetRestoredFileContent(Docset docset, SourceInfo<string> url)
        {
            return GetRestoredFileContent(docset.DocsetPath, url, docset.FallbackDocset?.DocsetPath);
        }

        public static (string localPath, string content, string etag) GetRestoredFileContent(string docsetPath, SourceInfo<string> url, string fallbackDocset = null)
        {
            var fromUrl = UrlUtility.IsHttp(url);
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
                    fullPath = Path.Combine(fallbackDocset, url);
                    if (File.Exists(fullPath))
                    {
                        return (fullPath, File.ReadAllText(fullPath), null);
                    }
                }

                throw Errors.FileNotFound(url).ToException();
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
            Debug.Assert(UrlUtility.IsHttp(url));

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

            var root = acquired is null;
            acquired = acquired ?? new Dictionary<(string remote, string branch, string commit), (string path, DependencyGit git)>();

            var successed = true;
            try
            {
                foreach (var gitVersion in dependencyLock.Git)
                {
                    var (remote, branch, _) = UrlUtility.SplitGitUrl(gitVersion.Key);
                    if (!acquired.ContainsKey((remote, branch, gitVersion.Value.Commit/*commit*/)))
                    {
                        var (path, git) = AcquireGit(remote, branch, gitVersion.Value.Commit, LockType.Shared);
                        acquired[(remote, branch, gitVersion.Value.Commit/*commit*/)] = (path, git);
                    }

                    Create(gitVersion.Value, acquired);
                }

                return new RestoreMap(acquired)
                {
                    DependencyLock = dependencyLock,
                };
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
