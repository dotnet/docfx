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

        public GitLock GitLock { get; }

        public RestoreGitMap(GitLock gitLock)
        {
            GitLock = gitLock;
            _sharedLocks = new List<SharedAndExclusiveLock>();
            CreateCore(GitLock);

            void CreateCore(GitLock dependencyGitLock)
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

        public static (string path, string commit) GetRestoreGitPath(GitLock gitLock, PackageUrl url, string docsetPath, bool bare)
        {
            Debug.Assert(gitLock != null);

            switch (url.Type)
            {
                case PackageType.Folder:
                    var fullPath = Path.Combine(docsetPath, url.Path);
                    if (Directory.Exists(fullPath))
                    {
                        return (fullPath, default);
                    }

                    // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                    // TODO: populate source info
                    throw Errors.NeedRestore(url.Path).ToException();

                case PackageType.Git:
                    var gitVersion = gitLock.GetGitVersion(url.Remote, url.Branch);

                    if (gitVersion == null || gitVersion.Commit == null)
                    {
                        throw Errors.NeedRestore($"{url}").ToException();
                    }

                    var path = AppData.GetGitDir(url.Remote);

                    if (!bare)
                    {
                        path = Path.Combine(path, "1");
                    }

                    if (!Directory.Exists(path))
                    {
                        throw Errors.NeedRestore($"{url}").ToException();
                    }

                    return (path, gitVersion.Commit);

                default:
                    throw new NotSupportedException($"Unknown package url: '{url}'");
            }
        }
    }
}
