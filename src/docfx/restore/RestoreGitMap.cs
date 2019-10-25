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
        private readonly List<InterProcessReaderWriterLock> _sharedLocks = new List<InterProcessReaderWriterLock>();
        private readonly DependencyLockProvider _dependencyLockProvider;

        private RestoreGitMap(DependencyLockProvider dependencyLockProvider, string docsetPath)
        {
            Debug.Assert(dependencyLockProvider != null);
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));

            _docsetPath = docsetPath;
            _dependencyLockProvider = dependencyLockProvider;

            foreach (var (url, _, _) in _dependencyLockProvider.ListAll())
            {
                var sharedLock = InterProcessReaderWriterLock.CreateReaderLock(url);
                _sharedLocks.Add(sharedLock);
            }
        }

        public (string path, string commit) GetRestoreGitPath(string url, string branch, bool bare /* remove this flag once all dependency repositories are bare cloned*/)
        {
            var gitLock = _dependencyLockProvider.GetGitLock(url, branch);

            if (gitLock is null || gitLock.Commit is null)
            {
                throw Errors.NeedRestore($"{url}#{branch}").ToException();
            }

            var path = AppData.GetGitDir(url);

            if (!bare)
            {
                path = Path.Combine(path, "1");
            }

            if (!Directory.Exists(path))
            {
                throw Errors.NeedRestore($"{url}#{branch}").ToException();
            }

            return (path, gitLock.Commit);
        }

        public bool IsBranchRestored(string remote, string branch)
        {
            var gitLock = _dependencyLockProvider.GetGitLock(remote, branch);

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
        public static RestoreGitMap Create(string docsetPath, string locale)
        {
            var dependencyLockProvider = DependencyLockProvider.CreateFromAppData(docsetPath, locale);

            return new RestoreGitMap(dependencyLockProvider, docsetPath);
        }
    }
}
