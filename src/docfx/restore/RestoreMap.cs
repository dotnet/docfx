// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RestoreMap
    {
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_gitPath = new ConcurrentDictionary<string, Lazy<string>>();
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_downloadPath = new ConcurrentDictionary<string, Lazy<string>>();

        private readonly string _docsetPath;

        public RestoreMap(string docsetPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            _docsetPath = docsetPath;
        }

        public string GetGitRestorePath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var gitRestorePath = s_gitPath.GetOrAdd(url, new Lazy<string>(FindLastModifiedGitRepository)).Value;

            if (!Directory.Exists(gitRestorePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return gitRestorePath;

            string FindLastModifiedGitRepository()
            {
                var (remote, branch) = GitUtility.GetGitRemoteInfo(url);
                var restoreDir = AppData.GetGitDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(url).ToException();
                }

                return Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(HrefUtility.EscapeUrlSegment(branch)))
                    .OrderByDescending(f => new DirectoryInfo(f).LastWriteTimeUtc)
                    .FirstOrDefault();
            }
        }

        public string GetFileRestorePath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            if (!HrefUtility.IsHttpHref(url))
            {
                // directly return the relative path
                var fullPath = Path.Combine(_docsetPath, url);
                return File.Exists(fullPath) ? fullPath : throw Errors.FileNotFound(_docsetPath, url).ToException();
            }

            var downloadPath = s_downloadPath.GetOrAdd(url, new Lazy<string>(FindLastModifiedFile)).Value;

            if (!File.Exists(downloadPath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return downloadPath;

            string FindLastModifiedFile()
            {
                // get the file path from restore map
                var restoreDir = AppData.GetFileDownloadDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(url).ToException();
                }

                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                       .FirstOrDefault();
            }
        }
    }
}
