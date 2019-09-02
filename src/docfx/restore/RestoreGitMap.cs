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

        public GitLock GitLock { get; private set; }

        public static RestoreGitMap Create(GitLock gitLock)
        {
            Debug.Assert(gitLock != null);

            var result = new RestoreGitMap { GitLock = gitLock };
            result.CreateCore(gitLock);

            return result;
        }

        public void Dispose()
        {
            foreach (var sharedLock in _sharedLocks)
            {
                sharedLock.Dispose();
            }
        }

        // todo: will be removed after we faltten the restored git map
        public RestoreGitMap GetSubRestoreGitMap(PackageUrl url)
        {
            var subGitLock = GitLock.GetGitVersion(url.Remote, url.Branch);
            return new RestoreGitMap { GitLock = subGitLock };
        }

        public (string path, string commit) GetRestoreGitPath(PackageUrl url, string docsetPath, bool bare)
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
                    var gitVersion = GitLock.GetGitVersion(url.Remote, url.Branch);

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

        private void CreateCore(GitLock dependencyGitLock)
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
}
