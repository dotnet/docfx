// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Concurrent;
using System.IO;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal sealed class GitCommitProvider : IDisposable
    {
        private readonly ConcurrentDictionary<string, FileCommitProvider> _fileCommitProvidersByRepoPath = new ConcurrentDictionary<string, FileCommitProvider>();

        public (Repository? repo, string? pathToRepo, GitCommit[] commits) GetCommitHistory(Document document, string? committish = null)
        {
            var repository = document.Docset.GetRepository(document.FilePath.Path);
            return GetCommitHistory(Path.Combine(document.Docset.DocsetPath, document.FilePath.GetPathToOrigin()), repository, committish);
        }

        public (Repository? repo, string? pathToRepo, GitCommit[] commits) GetCommitHistory(Docset docset, string filePath)
        {
            var repo = docset.GetRepository(filePath);
            if (repo is null)
                return (null, null, Array.Empty<GitCommit>());

            return GetCommitHistory(Path.Combine(docset.DocsetPath, filePath), repo);
        }

        public (Repository? repo, string? pathToRepo, GitCommit[] commits) GetCommitHistory(string fullPath, Repository? repo, string? committish = null)
        {
            if (repo is null)
                return (null, null, Array.Empty<GitCommit>());

            using (Telemetry.TrackingOperationTime(TelemetryName.LoadCommitHistory))
            {
                var pathToRepo = PathUtility.NormalizeFile(Path.GetRelativePath(repo.Path, fullPath));
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
