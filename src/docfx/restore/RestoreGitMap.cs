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
        private readonly List<SharedAndExclusiveLock> _sharedLocks = new List<SharedAndExclusiveLock>();

        public Dictionary<PackageUrl, DependencyGitLock> GitLock { get; private set; }

        public RestoreGitMap(Dictionary<PackageUrl, DependencyGitLock> gitLock)
        {
            Debug.Assert(gitLock != null);

            GitLock = gitLock;

            foreach (var (packageUrl, _) in gitLock)
            {
                var sharedLock = new SharedAndExclusiveLock(packageUrl.Remote, shared: true);
                _sharedLocks.Add(sharedLock);
            }
        }

        public (string path, string commit) GetRestoreGitPath(PackageUrl url, string docsetPath, bool bare /* remove this flag once all dependency repositories are bare cloned*/)
        {
            Debug.Assert(GitLock != null);

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
                    var gitLock = GitLock.GetGitLock(url);

                    if (gitLock == null || gitLock.Commit == null)
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

                    return (path, gitLock.Commit);

                default:
                    throw new NotSupportedException($"Unknown package url: '{url}'");
            }
        }

        public void Dispose()
        {
            foreach (var sharedLock in _sharedLocks)
            {
                sharedLock.Dispose();
            }
        }
    }
}
