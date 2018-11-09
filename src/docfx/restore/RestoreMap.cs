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

        public string GetGitRepositoryPath(string remote)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            var gitRestorePath = s_gitPath.GetOrAdd(remote, new Lazy<string>(FindLastModifiedGitRepository)).Value;

            if (!Directory.Exists(gitRestorePath))
            {
                throw Errors.NeedRestore(remote).ToException();
            }

            return gitRestorePath;

            string FindLastModifiedGitRepository()
            {
                var (url, branch) = GitUtility.GetGitRemoteInfo(remote);
                var restoreDir = AppData.GetGitDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(remote).ToException();
                }

                return Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                    .Select(f => PathUtility.NormalizeFolder(f))
                    .Where(f => f.EndsWith($"{PathUtility.Encode(branch)}/"))
                    .OrderByDescending(f => new DirectoryInfo(f).LastWriteTimeUtc)
                    .FirstOrDefault();
            }
        }

        public string GetFileDownloadPath(string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (!HrefUtility.IsHttpHref(path))
            {
                // directly return the relative path
                var fullPath = Path.Combine(_docsetPath, path);
                return File.Exists(fullPath) ? fullPath : throw Errors.FileNotFound(_docsetPath, path).ToException();
            }

            var downloadPath = s_downloadPath.GetOrAdd(path, new Lazy<string>(FindLastModifiedDownload)).Value;

            if (!File.Exists(downloadPath))
            {
                throw Errors.NeedRestore(path).ToException();
            }

            return downloadPath;

            string FindLastModifiedDownload()
            {
                // get the file path from restore map
                var restoreDir = AppData.GetFileDownloadDir(path);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(path).ToException();
                }

                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                       .FirstOrDefault();
            }
        }
    }
}
