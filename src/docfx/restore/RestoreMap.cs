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
        private static readonly ConcurrentDictionary<string, Lazy<string>> s_mappings = new ConcurrentDictionary<string, Lazy<string>>();
        private readonly string _docsetPath;

        public RestoreMap(string docsetPath)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            _docsetPath = docsetPath;
        }

        public string GetGitRestorePath(string url)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var gitRestorePath = s_mappings.GetOrAdd(
                $"{url}",
                new Lazy<string>(() =>
                {
                    var (remote, branch) = GitUtility.GetGitRemoteInfo(url);
                    var restoreDir = AppData.GetGitDir(remote);

                    if (!Directory.Exists(restoreDir))
                    {
                        throw Errors.NeedRestore(url).ToException();
                    }

                    return Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                        .Select(f => PathUtility.NormalizeFolder(f))
                        .Where(f => f.EndsWith($"{HrefUtility.EscapeUrlSegment(branch)}/"))
                        .OrderByDescending(f => new DirectoryInfo(f).LastAccessTimeUtc)
                        .FirstOrDefault();
                })).Value;

            if (!Directory.Exists(gitRestorePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return gitRestorePath;
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

            var urlRestorePath = s_mappings.GetOrAdd(
                $"{_docsetPath}:{url}",
                new Lazy<string>(() =>
                {
                    // get the file path from restore map
                    var restoreDir = AppData.GetFileDownloadDir(url);

                    if (!Directory.Exists(restoreDir))
                    {
                        throw Errors.NeedRestore(url).ToException();
                    }

                    return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                           .OrderByDescending(f => new FileInfo(f)
                           .LastAccessTimeUtc).FirstOrDefault();
                })).Value;

            if (!File.Exists(urlRestorePath))
            {
                throw Errors.NeedRestore(url).ToException();
            }

            return urlRestorePath;
        }
    }
}
