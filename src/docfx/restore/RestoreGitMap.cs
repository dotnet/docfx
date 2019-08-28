// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RestoreGitMap : IDisposable
    {
        private readonly List<SharedAndExclusiveLock> _sharedLocks;

        public DependencyGitLock GitLock { get; }

        public RestoreGitMap(DependencyGitLock gitLock)
        {
            GitLock = gitLock;
            _sharedLocks = new List<SharedAndExclusiveLock>();
            CreateCore(GitLock);

            void CreateCore(DependencyGitLock dependencyGitLock)
            {
                foreach (var gitVersion in dependencyGitLock.Git)
                {
                    var (remote, branch, _) = UrlUtility.SplitGitUrl(gitVersion.Key);
                    var sharedLock = new SharedAndExclusiveLock(remote, shared: true);

                    _sharedLocks.Add(sharedLock);
                    CreateCore(gitVersion.Value);
                }
            }
        }

        public void Dispose()
        {
            foreach (var sharedLock in _sharedLocks)
            {
                sharedLock.Dispose();
            }
        }

        public static (string path, string commit) GetRestoreGitPath(DependencyGitLock gitLock, string remote, string branch, string docsetPath, bool bare)
        {
            Debug.Assert(gitLock != null);

            if (!UrlUtility.IsHttp(remote))
            {
                var fullPath = Path.Combine(docsetPath, remote);
                if (Directory.Exists(fullPath))
                {
                    return (fullPath, default);
                }

                // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                // TODO: populate source info
                throw Errors.NeedRestore(remote).ToException();
            }

            var gitVersion = gitLock.GetGitLock(remote, branch);

            if (gitVersion == null || gitVersion.Commit == null)
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            var path = AppData.GetGitDir(remote);

            if (!bare)
            {
                path = Path.Combine(path, "1");
            }

            if (!Directory.Exists(path))
            {
                throw Errors.NeedRestore($"{remote}#{branch}").ToException();
            }

            return (path, gitVersion.Commit);
        }
    }
}
