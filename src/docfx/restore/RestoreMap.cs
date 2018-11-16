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
        private static readonly ConcurrentDictionary<(string remote, string branch), Lazy<string>> s_gitPath = new ConcurrentDictionary<(string remote, string branch), Lazy<string>>();
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_downloadPath = new ConcurrentDictionary<string, Lazy<string>>();

        public static string GetGitRestorePath(string url)
        {
            if (!TryGetGitRestorePath(url, out var result))
            {
                throw Errors.NeedRestore(url).ToException();
            }
            return result;
        }

        public static bool TryGetGitRestorePath(string url, out string result)
        {
            var (remote, branch) = GitUtility.GetGitRemoteInfo(url);
            return TryGetGitRestorePath(remote, branch, out result);
        }

        public static bool TryGetGitRestorePath(string remote, string branch, out string result)
        {
            result = s_gitPath.GetOrAdd((remote, branch), new Lazy<string>(FindLastModifiedGitRepository)).Value;

            // GetOrAdd and TryRemove together are not atomic operation,
            // but because we only call TryGetGitRestorePath in implicit restore,
            // it is very unlikely to cause a problem.
            if (result == null)
            {
                s_gitPath.TryRemove((remote, branch), out _);
            }

            return Directory.Exists(result);

            string FindLastModifiedGitRepository()
            {
                var restoreDir = AppData.GetGitDir(remote);

                if (!Directory.Exists(restoreDir))
                {
                    return null;
                }

                return Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(HrefUtility.EscapeUrlSegment(branch)))
                    .OrderByDescending(f => new DirectoryInfo(f).LastWriteTimeUtc)
                    .FirstOrDefault();
            }
        }

        public static string GetFileRestorePath(this Docset docset, string url)
        {
            return GetFileRestorePath(docset.FallbackDocset?.DocsetPath ?? docset.DocsetPath, url);
        }

        public static string GetFileRestorePath(string docsetPath, string url)
        {
            if (!HrefUtility.IsHttpHref(url))
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, url);
                return File.Exists(fullPath) ? fullPath : throw Errors.FileNotFound(docsetPath, url).ToException();
            }

            if (!TryGetFileRestorePath(url, out var result))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return result;
        }

        public static bool TryGetFileRestorePath(string url, out string result)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));
            Debug.Assert(HrefUtility.IsHttpHref(url));

            result = s_downloadPath.GetOrAdd(url, new Lazy<string>(FindLastModifiedFile)).Value;
            if (result == null)
            {
                s_downloadPath.TryRemove(url, out _);
            }

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
