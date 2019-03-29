// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, FileCommitProvider> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, FileCommitProvider>();

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetCommitHistory(Document document, string committish = null)
           => GetCommitHistory(Path.Combine(document.Docset.DocsetPath, document.FilePath), document.Repository, committish);

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetCommitHistory(string fullPath, Repository repo, string committish = null)
        {
            if (repo is null)
                return default;

            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));
            return (repo, pathToRepo, GetCommitProvider(repo).GetCommitHistory(pathToRepo, committish));
        }

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetCommitHistoryNoCache(Docset docset, string filePath, int top, string committish = null)
        {
            var repo = docset.GetRepository(filePath);
            if (repo is null)
                return default;

            var fullPath = Path.Combine(docset.DocsetPath, filePath);
            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));

            return (repo, pathToRepo, GetCommitProvider(repo).GetCommitHistoryNoCache(pathToRepo, top, committish));
        }

        public void SaveGitCommitCache()
        {
            foreach (var p in _fileCommitProvidersByRepoPath.Values)
            {
                p.SaveCache();
            }
        }

        public void Dispose()
        {
            foreach (var provider in _fileCommitProvidersByRepoPath.Values)
            {
                provider.Dispose();
            }
        }

        private FileCommitProvider GetCommitProvider(Repository repo)
        {
            return _fileCommitProvidersByRepoPath.GetOrAdd(
                repo.Path,
                _ => new FileCommitProvider(repo.Path, AppData.GetCommitCachePath(repo.Remote)));
        }
    }
}
