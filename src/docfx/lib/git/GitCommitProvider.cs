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
        private readonly ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, Repository repo)>>> _commitProviders = new ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, Repository repo)>>>();

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
            foreach (var value in _commitProviders.Values)
            {
                if (value.IsValueCreated)
                {
                    var (provider, repo) = await value.Value;
                    await GitCommitCacheProvider.SaveCache(repo, provider.BuildCache());
                }
            }
        }

        public void Dispose()
        {
            foreach (var value in _commitProviders.Values)
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
            var (provider, _) = await _commitProviders.GetOrAdd(
                repo.Path,
                p => new Lazy<Task<(FileCommitProvider provider, Repository repo)>>(async () =>
                {
                    return (new FileCommitProvider(repo.Path, await GitCommitCacheProvider.LoadCommitCache(repo)), repo);
                })).Value;

            return provider;
        }

        private Repository GetRepository(string fullPath)
        {
            return !string.IsNullOrEmpty(fullPath)
                ? _repositoryByFolder.GetOrAdd(fullPath, p =>
                {
                    if (GitUtility.IsRepo(p))
                        return Repository.Create(p);

                    return GetRepository(Path.GetDirectoryName(fullPath));
                })
                : null;
        }
    }
}
