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
            : this(RestoreLocker.Load(docsetPath).Result)
        {
        }

        public RestoreMap(RestoreLock restoreLock)
        {
            Debug.Assert(restoreLock != null);

            _restoreLock = restoreLock;
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

        public string GetUrlRestorePath(string docsetPath, string path)
        {
            Debug.Assert(!string.IsNullOrEmpty(path));

            if (!HrefUtility.IsHttpHref(path))
            {
                // directly return the relative path
                return Path.Combine(docsetPath, path);
            }

            // get the file path from restore map
            if (TryGetUrlRestorePath(path, out var restorePath) && File.Exists(restorePath))
            {
                return restorePath;
            }

            throw Errors.UrlRestorePathNotFound(path).ToException();
        }
    }
}
