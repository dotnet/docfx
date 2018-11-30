// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, Repository repo)>>> _commitProviders = new ConcurrentDictionary<string, Lazy<Task<(FileCommitProvider provider, Repository repo)>>>();

        public async Task<List<GitCommit>> GetCommitHistory(Repository repo, string file, string committish = null)
        {
            var fileCommitProvider = await GetGitCommitProvider(repo);

            return fileCommitProvider.GetCommitHistory(file, committish);
        }

        public async Task SaveGitCommitCache()
        {
            foreach (var value in _commitProviders.Values)
            {
                var (provider, repo) = await value.Value;
                await GitCommitCacheProvider.SaveCache(repo, provider.BuildCache());
            }
        }

        public void Dispose()
        {
            foreach (var value in _commitProviders.Values)
            {
                value.Value.Result.provider.Dispose();
            }
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
    }
}
