// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, Repository> _repositoryByFolder = new ConcurrentDictionary<string, Repository>();
        private readonly ConcurrentDictionary<string, Lazy<(FileCommitProvider provider, string repoRemote)>> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, Lazy<(FileCommitProvider provider, string repoRemote)>>();

        public Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(Document document, string committish = null)
            => GetCommitHistory(Path.Combine(document.Docset.DocsetPath, document.FilePath), committish);

        public async Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(string fullPath, string committish = null)
        {
            var (repo, pathToRepo) = GetRepository(fullPath);
            if (repo == null)
                return default;

            return (repo, pathToRepo, await GetGitCommitProvider(repo).GetCommitHistory(pathToRepo, committish));
        }

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetDeletedFileCommitHistory(string fullPath, int top, string committish = null)
        {
            var (repo, pathToRepo) = GetRepository(fullPath);
            if (repo == null)
                return default;

            return (repo, pathToRepo, GetGitCommitProvider(repo).GetDeletedFileCommitHistory(pathToRepo, top, committish));
        }

        public async Task SaveGitCommitCache()
        {
            foreach (var value in _fileCommitProvidersByRepoPath.Values)
            {
                if (value.IsValueCreated)
                {
                    await GitCommitCacheProvider.SaveCache(value.Value.repoRemote, await value.Value.provider.BuildCache());
                }
            }
        }

        public void Dispose()
        {
            foreach (var value in _fileCommitProvidersByRepoPath.Values)
            {
                if (value.IsValueCreated)
                {
                    value.Value.provider.Dispose();
                }
            }
        }

        private FileCommitProvider GetGitCommitProvider(Repository repo)
            => _fileCommitProvidersByRepoPath.GetOrAdd(repo.Path, new Lazy<(FileCommitProvider provider, string repoRemote)>(() => (new FileCommitProvider(repo.Path, new Lazy<Task<ConcurrentDictionary<string, Dictionary<(long commit, long blob), (long[] commitHistory, int lruOrder)>>>>(() => GitCommitCacheProvider.LoadCommitCache(repo.Remote))), repo.Remote))).Value.provider;

        private (Repository repo, string pathToRepo) GetRepository(string fullPath)
        {
            var repo = GetRepositoryInternal(fullPath);
            if (repo == null)
                return default;
            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));

            return (repo, pathToRepo);
        }

        private Repository GetRepositoryInternal(string fullPath)
        {
            if (GitUtility.IsRepo(fullPath))
                return Repository.Create(fullPath);

            var parent = Path.GetDirectoryName(fullPath);
            return !string.IsNullOrEmpty(parent)
                ? _repositoryByFolder.GetOrAdd(parent, GetRepositoryInternal)
                : null;
        }
    }
}
