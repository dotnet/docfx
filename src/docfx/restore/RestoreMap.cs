// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class RestoreMap
    {
        private static readonly ConcurrentDictionary<(string remote, string branch, string commit), Lazy<string>> s_gitPath = new ConcurrentDictionary<(string remote, string branch, string commit), Lazy<string>>();
        private static readonly ConcurrentDictionary<(string url, string version), Lazy<string>> s_downloadPath = new ConcurrentDictionary<(string url, string version), Lazy<string>>();

        /// <summary>
        /// Get restored git repository path with url and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static (string path, DependencyLockModel subDependencyLock) GetGitRestorePath(string url, DependencyLockModel dependencyLock)
        {
            var (remote, branch) = HrefUtility.SplitGitHref(url);
            return GetGitRestorePath(remote, branch, dependencyLock);
        }

        /// <summary>
        /// Get restored git repository path with remote, branch and dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static (string path, DependencyLockModel subDependencyLock) GetGitRestorePath(string remote, string branch, DependencyLockModel dependencyLock)
        {
            Debug.Assert(dependencyLock != null);

            var gitVersion = dependencyLock.GetGitLock(remote, branch);

            if (gitVersion == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            if (!TryGetGitRestorePath(remote, branch, gitVersion, out var result))
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            return (result, gitVersion);
        }

        /// <summary>
        /// Try get git dependency repository path with remote, branch and dependency version
        /// If the dependency version is null, get the latest one(order by last write time)
        /// If the dependency version is not null, get the one matched with the version(commit).
        /// </summary>
        public static bool TryGetGitRestorePath(string remote, string branch, DependencyVersion dependencyVersion, out string result)
        {
            var commit = dependencyVersion?.Commit;
            var locked = !string.IsNullOrEmpty(commit);
            result = s_gitPath.AddOrUpdate(
                (remote, branch, commit),
                new Lazy<string>(FindGitRepository),
                (_, existing) => existing.Value != null ? existing : new Lazy<string>(FindGitRepository)).Value;

            return Directory.Exists(result);

            string FindGitRepository()
            {
                var repoPath = AppData.GetGitDir(remote);

                if (!Directory.Exists(repoPath))
                {
                    return null;
                }

                // return specificed version
                if (locked)
                {
                    if (TryGetLockedWorkTreePath(repoPath, true, out var workTree) || TryGetLockedWorkTreePath(repoPath, false, out workTree))
                    {
                        return workTree;
                    }

                    return null;
                }

                // return the latest version
                return (
                    from path in Directory.GetDirectories(repoPath, "*", SearchOption.TopDirectoryOnly)
                    let name = Path.GetFileName(path)
                    where GitUtility.IsWorkTreeCheckoutComplete(repoPath, name) && name.StartsWith(RestoreGit.GetWorkTreeHeadPrefix(branch))
                    orderby new DirectoryInfo(path).LastWriteTimeUtc
                    select path).FirstOrDefault();
            }

            bool TryGetLockedWorkTreePath(string root, bool isLocked, out string workTreePath)
            {
                var workTreeName = $"{RestoreGit.GetWorkTreeHeadPrefix(branch, isLocked)}{commit}";
                workTreePath = Path.Combine(root, workTreeName);
                if (Directory.Exists(workTreePath) && GitUtility.IsWorkTreeCheckoutComplete(root, workTreeName))
                {
                    return true;
                }

                return false;
            }
        }

        public static (bool fromUrl, string path) GetFileRestorePath(string docsetPath, string url, DependencyVersion dependencyVersion, string fallbackDocset = null)
        {
            var fromUrl = HrefUtility.IsHttpHref(url);
            if (!fromUrl)
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                if (File.Exists(fullPath))
                {
                    return (fromUrl, fullPath);
                }

                if (!string.IsNullOrEmpty(fallbackDocset))
                {
                    return GetFileRestorePath(fallbackDocset, url, dependencyVersion);
                }

                throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            if (!TryGetFileRestorePath(url, dependencyVersion, out var result))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (fromUrl, result);
        }

        public static bool TryGetFileRestorePath(string url, DependencyVersion dependencyVersion, out string result)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            var fileName = dependencyVersion?.Hash;
            var locked = !string.IsNullOrEmpty(fileName);
            result = s_downloadPath.AddOrUpdate(
                (url, fileName),
                new Lazy<string>(FindFile),
                (_, existing) => existing.Value != null ? existing : new Lazy<string>(FindFile)).Value;

            return File.Exists(result);

            string FindFile()
            {
                // get the file path from restore map
                var restoreDir = AppData.GetFileDownloadDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    return null;
                }

                // return specified version
                if (locked)
                {
                    return Path.Combine(restoreDir, fileName);
                }

                // return the latest version
                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                       .FirstOrDefault();
            }
        }
    }
}
