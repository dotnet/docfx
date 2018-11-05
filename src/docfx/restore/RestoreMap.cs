// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreMap
    {
        private readonly RestoreLock _restoreLock;
        private readonly string _docsetPath;

        public RestoreMap(string docsetPath)
            : this(docsetPath, RestoreLocker.Load(docsetPath).GetAwaiter().GetResult())
        {
        }

        public RestoreMap(string docsetPath, RestoreLock restoreLock)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            Debug.Assert(restoreLock != null);

            _docsetPath = docsetPath;
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

        public string GetUrlRestorePath(string path)
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
