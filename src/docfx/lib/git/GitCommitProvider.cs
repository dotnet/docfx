// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();
        private readonly ConcurrentDictionary<string, FileCommitProvider> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, FileCommitProvider>();

        public Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(Document document, string committish = null)
           => GetCommitHistory(Path.Combine(document.Docset.DocsetPath, document.FilePath), committish);

        public async Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(string fullPath, string committish = null)
        {
            var repo = GetRepository(fullPath);
            if (repo == null)
                return default;
            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));

            return (repo, pathToRepo, await GetCommitProvider(repo).GetCommitHistory(pathToRepo, committish));
        }

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetCommitHistoryNoCache(string fullPath, int top, string committish = null)
        {
            var repo = GetRepository(fullPath);
            if (repo == null)
                return default;
            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));

            return (repo, pathToRepo, GetCommitProvider(repo).GetCommitHistoryNoCache(pathToRepo, top, committish));
        }

        public Task SaveGitCommitCache()
        {
            return Task.WhenAll(_fileCommitProvidersByRepoPath.Values.Select(provider => provider.SaveCache()));
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

        private Repository GetRepository(string fullPath)
        {
            if (GitUtility.IsRepo(fullPath))
                return Repository.Create(fullPath);

            var parent = Path.GetDirectoryName(fullPath);
            return !string.IsNullOrEmpty(parent)
                ? _repositoryByFolder.GetOrAdd(parent, GetRepository)
                : null;
        }
    }
}
