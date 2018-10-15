// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreMap
    {
        private readonly RestoreLock _restoreLock;

        public RestoreMap(string docsetPath)
            : this(RestoreLocker.Load(docsetPath).GetAwaiter().GetResult())
        {
        }

        public RestoreMap(RestoreLock restoreLock)
        {
            Debug.Assert(restoreLock != null);

            _restoreLock = restoreLock;
        }

        public string GetGitRestorePath(string remote)
        {
            var (url, _) = GitUtility.GetGitRemoteInfo(remote);
            var restoreDir = RestoreGit.GetRestoreRootDir(url);
            if (_restoreLock.Git.TryGetValue(remote, out var workTreeHead) && !string.IsNullOrEmpty(workTreeHead))
            {
                var result = RestoreWorkTree.GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                if (Directory.Exists(result))
                {
                    return result;
                }
            }

            throw Errors.NeedRestore(remote).ToException();
        }

        public string GetUrlRestorePath(string docsetPath, string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (!HrefUtility.IsHttpHref(path))
            {
                // directly return the relative path
                var fullPath = Path.Combine(docsetPath, path);
                return File.Exists(fullPath) ? fullPath : throw Errors.FileNotFound(docsetPath, path).ToException();
            }

            // get the file path from restore map
            var restoreDir = RestoreUrl.GetRestoreRootDir(path);
            if (_restoreLock.Url.TryGetValue(path, out var version) && !string.IsNullOrEmpty(version))
            {
                var result = RestoreUrl.GetRestoreVersionPath(restoreDir, version);
                if (File.Exists(result))
                {
                    return result;
                }
            }

            throw Errors.NeedRestore(path).ToException();
        }
    }
}
