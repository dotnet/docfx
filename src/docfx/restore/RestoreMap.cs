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
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_downloadPath = new ConcurrentDictionary<string, Lazy<string>>();

        public static (string path, DependencyLock subDependencyLock) GetGitRestorePath(string url, DependencyLock dependencyLock)
        {
            var (remote, branch) = HrefUtility.SplitGitHref(url);
            return GetGitRestorePath(remote, branch, dependencyLock);
        }

        public static (string path, DependencyLock subDependencyLock) GetGitRestorePath(string remote, string branch, DependencyLock dependencyLock)
        {
            if (!TryGetGitRestorePath(remote, branch, dependencyLock, out var result, out var subDependencyLock))
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            return (result, subDependencyLock);
        }

        public static bool TryGetGitRestorePath(string remote, string branch, DependencyLock dependencyLock, out string result, out DependencyLock subDependencyLock)
        {
            subDependencyLock = dependencyLock?.GetGitLock(remote, branch);
            var commit = subDependencyLock?.Commit;
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

                return (
                    from path in Directory.GetDirectories(repoPath, "*", SearchOption.TopDirectoryOnly)
                    let name = Path.GetFileName(path)
                    where GitUtility.IsWorkTreeCheckoutComplete(repoPath, name) &&
                        ((locked && name == $"{RestoreGit.GetWorkTreeHeadPrefix(branch, locked)}-{commit}") ||
                        (!locked && name.StartsWith(RestoreGit.GetWorkTreeHeadPrefix(branch))))
                    orderby new DirectoryInfo(path).LastWriteTimeUtc
                    select path).FirstOrDefault();
            }
        }

        public static (bool fromUrl, string path) GetFileRestorePath(string docsetPath, string url, string fallbackDocset = null)
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
                    return GetFileRestorePath(fallbackDocset, url);
                }

                throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            if (!TryGetFileRestorePath(url, out var result))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return (fromUrl, result);
        }

        public static bool TryGetFileRestorePath(string url, out string result)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            result = s_downloadPath.AddOrUpdate(
                url,
                new Lazy<string>(FindLastModifiedFile),
                (_, existing) => existing.Value != null ? existing : new Lazy<string>(FindLastModifiedFile)).Value;

            return File.Exists(result);

            string FindLastModifiedFile()
            {
                // get the file path from restore map
                var restoreDir = AppData.GetFileDownloadDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    return null;
                }

                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                       .FirstOrDefault();
            }
        }
    }
}
