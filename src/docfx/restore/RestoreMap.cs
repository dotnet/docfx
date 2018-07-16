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
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            _restoreLock = RestoreLocker.Load(docsetPath).Result;
        }

        public bool TryGetGitRestorePath(string remote, out string restorePath)
        {
            var (url, _) = GitUtility.GetGitRemoteInfo(remote);
            var restoreDir = RestoreGit.GetRestoreRootDir(url);
            if (_restoreLock.Git.TryGetValue(remote, out var workTreeHead) && !string.IsNullOrEmpty(workTreeHead))
            {
                restorePath = RestoreGit.GetRestoreWorkTreeDir(restoreDir, workTreeHead);
                return true;
            }

            restorePath = default;
            return false;
        }

        public bool TryGetUrlRestorePath(string remote, out string restorePath)
        {
            var restoreDir = RestoreUrl.GetRestoreRootDir(remote);
            if (_restoreLock.Url.TryGetValue(remote, out var version) && !string.IsNullOrEmpty(version))
            {
                restorePath = RestoreUrl.GetRestoreVersionPath(restoreDir, version);
                return true;
            }

            restorePath = default;
            return false;
        }

        public string GetUrlRestorePath(string remote)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));

            // get the file path from restore map
            if (TryGetUrlRestorePath(remote, out var restorePath) && File.Exists(restorePath))
            {
                return restorePath;
            }

            throw Errors.UrlRestorePathNotFound(remote).ToException();
        }
    }
}
