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
        private readonly ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, string repoRemote)>>> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, string repoRemote)>>>();

        public Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(Document document, string committish = null)
           => GetCommitHistory(Path.Combine(document.Docset.DocsetPath, document.FilePath), committish);

        public async Task<(Repository repo, string pathToRepo, List<GitCommit> commits)> GetCommitHistory(string fullPath, string committish = null)
        {
            var repo = GetRepository(fullPath);
            if (repo == null)
                return default;
            var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));
            return (repo, pathToRepo, await GetCommitHistory(repo, pathToRepo, committish));
        }

        public async Task SaveGitCommitCache()
        {
            foreach (var value in _fileCommitProvidersByRepoPath.Values)
            {
                if (value.IsValueCreated)
                {
                    var (provider, repoRemote) = await value.Value;
                    await GitCommitCacheProvider.SaveCache(repoRemote, provider.BuildCache());
                }
            }
        }

        public void Dispose()
        {
            foreach (var value in _fileCommitProvidersByRepoPath.Values)
            {
                if (value.IsValueCreated && value.Value.IsCompletedSuccessfully)
                {
                    value.Value.Result.provider.Dispose();
                }
            }
        }

        private async Task<List<GitCommit>> GetCommitHistory(Repository repo, string file, string committish = null)
        {
            var fileCommitProvider = await GetGitCommitProvider(repo);

            return fileCommitProvider.GetCommitHistory(file, committish);
        }

        private async Task<FileCommitProvider> GetGitCommitProvider(Repository repo)
        {
            var (provider, _) = await _fileCommitProvidersByRepoPath.GetOrAdd(
                repo.Path,
                p => new Lazy<Task<(FileCommitProvider provider, string repoRemote)>>(async () =>
                {
                    return (new FileCommitProvider(repo.Path, await GitCommitCacheProvider.LoadCommitCache(repo.Remote)), repo.Remote);
                })).Value;

            return provider;
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
