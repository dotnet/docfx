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
        private readonly RepositoryProvider _repositoryProvider;
        private readonly ConcurrentDictionary<string, FileCommitProvider> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, FileCommitProvider>();

        public GitCommitProvider(RepositoryProvider repositoryProvider)
        {
            _repositoryProvider = repositoryProvider;
        }

        public (Repository repo, string pathToRepo, List<GitCommit> commits) GetCommitHistory(FilePath file, string committish = null)
        {
            var (repo, pathToRepo) = _repositoryProvider.GetRepository(file);
            if (repo is null)
                return default;

            using (Telemetry.TrackingOperationTime(TelemetryName.LoadCommitHistory))
            {
                return (repo, pathToRepo, GetCommitProvider(repo).GetCommitHistory(pathToRepo, committish));
            }
        }

        public void Save()
        {
            foreach (var p in _fileCommitProvidersByRepoPath.Values)
            {
                p.Save();
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
                _ => new FileCommitProvider(repo, AppData.GetCommitCachePath(repo.Remote)));
        }
    }
}
