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

        public string GetGitRestorePath(string remote)
        {
            var gitRestorePath = s_mappings.GetOrAdd(
            $"{remote}",
            new Lazy<string>(() =>
            {
                Debug.Assert(!string.IsNullOrEmpty(remote));
                var (url, branch) = GitUtility.GetGitRemoteInfo(remote);
                var restoreDir = RestoreGit.GetRestoreRootDir(url);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(remote).ToException();
                }

                return Directory.EnumerateDirectories(restoreDir, "*", SearchOption.TopDirectoryOnly)
                    .Select(f => PathUtility.NormalizeFolder(f))
                    .Where(f => f.EndsWith($"{PathUtility.Encode(branch)}/"))
                    .OrderByDescending(f => new DirectoryInfo(f).LastAccessTimeUtc)
                    .FirstOrDefault();
            })).Value;

            if (!Directory.Exists(gitRestorePath))
            {
                throw Errors.NeedRestore(remote).ToException();
            }

            return gitRestorePath;
        }

        public string GetUrlRestorePath(string path)
        {
            var urlRestorePath = s_mappings.GetOrAdd(
            $"{_docsetPath}:{path}",
            new Lazy<string>(() =>
            {
                Debug.Assert(!string.IsNullOrEmpty(path));

                if (!HrefUtility.IsHttpHref(path))
                {
                    // directly return the relative path
                    var fullPath = Path.Combine(_docsetPath, path);
                    return File.Exists(fullPath) ? fullPath : throw Errors.FileNotFound(_docsetPath, path).ToException();
                }

                // get the file path from restore map
                var restoreDir = RestoreUrl.GetRestoreRootDir(path);

                if (!Directory.Exists(restoreDir))
                {
                    throw Errors.NeedRestore(path).ToException();
                }

                return Directory.EnumerateFiles(restoreDir, "*", SearchOption.TopDirectoryOnly)
                       .OrderByDescending(f => new FileInfo(f)
                       .LastAccessTimeUtc).FirstOrDefault();
            })).Value;

            if (!File.Exists(urlRestorePath))
            {
                throw Errors.NeedRestore(path).ToException();
            }

            return urlRestorePath;
        }
    }
}
