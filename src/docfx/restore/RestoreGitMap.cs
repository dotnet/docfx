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
        private readonly string _docsetPath;
        private readonly List<SharedAndExclusiveLock> _sharedLocks = new List<SharedAndExclusiveLock>();
        private readonly IReadOnlyDictionary<PackageUrl, DependencyGitLock> _dependencyGitLock;

        private RestoreGitMap(Dictionary<PackageUrl, DependencyGitLock> gitLock, string docsetPath)
        {
            Debug.Assert(gitLock != null);
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            _docsetPath = docsetPath;
            _dependencyGitLock = gitLock;

            foreach (var (packageUrl, _) in gitLock)
            {
                var sharedLock = new SharedAndExclusiveLock(packageUrl.Url, shared: true);
                _sharedLocks.Add(sharedLock);
            }
        }

        public (string path, string commit) GetRestoreGitPath(PackageUrl packageUrl, bool bare /* remove this flag once all dependency repositories are bare cloned*/)
        {
            Debug.Assert(_dependencyGitLock != null);

            switch (packageUrl.Type)
            {
                case PackageType.Folder:
                    var fullPath = Path.Combine(_docsetPath, packageUrl.Path);
                    if (Directory.Exists(fullPath))
                    {
                        return (fullPath, default);
                    }

                    // TODO: Intentionally don't fallback to fallbackDocset for git restore path,
                    // TODO: populate source info
                    throw Errors.NeedRestore(packageUrl.Path).ToException();

                case PackageType.Git:
                    var gitLock = _dependencyGitLock.GetGitLock(packageUrl);

                    if (gitLock is null || gitLock.Commit is null)
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    var path = AppData.GetGitDir(packageUrl.Url);

                    if (!bare)
                    {
                        path = Path.Combine(path, "1");
                    }

                    if (!Directory.Exists(path))
                    {
                        throw Errors.NeedRestore($"{packageUrl}").ToException();
                    }

                    return (path, gitLock.Commit);

                default:
                    throw new NotSupportedException($"Unknown package url: '{packageUrl}'");
            }
        }

        public bool IsBranchRestored(string remote, string branch)
        {
            var packageUrl = new PackageUrl(remote, branch);
            var gitLock = _dependencyGitLock.GetGitLock(packageUrl);

            if (gitLock is null || gitLock.Commit is null)
            {
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            foreach (var sharedLock in _sharedLocks)
            {
                sharedLock.Dispose();
            }
        }

        /// <summary>
        /// Acquired all shared git based on dependency lock
        /// The dependency lock must be loaded before using this method
        /// </summary>
        public static RestoreGitMap Create(string docsetPath, Config config, string locale)
        {
            var dependencyLockPath = string.IsNullOrEmpty(config.DependencyLock)
                    ? new SourceInfo<string>(AppData.GetDependencyLockFile(docsetPath, locale)) : config.DependencyLock;
            var dependencyLock = DependencyLockProvider.LoadGitLock(docsetPath, dependencyLockPath)
                ?? new Dictionary<PackageUrl, DependencyGitLock>();

            return new RestoreGitMap(dependencyLock, docsetPath);
        }
    }
}
