// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

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
    }
}
