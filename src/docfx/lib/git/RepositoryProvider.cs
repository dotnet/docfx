// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider : IDisposable
    {
        private readonly ErrorBuilder _errors;

        private readonly ConcurrentDictionary<string, Repository?> _repositories = new ConcurrentDictionary<string, Repository?>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<PathString, GitCommitLoader> _commitLoaders = new ConcurrentDictionary<PathString, GitCommitLoader>();

        public Repository? Repository { get; }

        public RepositoryProvider(ErrorBuilder errors, BuildOptions buildOptions, Config config)
        {
            _errors = errors;
            Repository = buildOptions.Repository;

            if (Repository != null && !config.DryRun && !buildOptions.IsLocalizedBuild)
            {
                GetCommitLoader(Repository).WarmUp();
            }
        }

        public (Repository? repository, PathString? pathToRepository) GetRepository(PathString fullPath)
        {
            Debug.Assert(Path.IsPathRooted(fullPath));

            var directory = Path.GetDirectoryName(fullPath);
            if (directory is null)
            {
                return default;
            }

            var repository = _repositories.GetOrAdd(directory, GetRepositoryCore);
            if (repository is null)
            {
                return default;
            }

            return (repository, new PathString(Path.GetRelativePath(repository.Path, fullPath)));
        }

        public (Repository? repo, PathString? pathToRepo, GitCommit[] commits) GetCommitHistory(PathString fullPath, string? committish = null)
        {
            var (repo, pathToRepo) = GetRepository(fullPath);
            if (repo is null || pathToRepo is null)
            {
                return (null, null, Array.Empty<GitCommit>());
            }

            return (repo, pathToRepo, GetCommitLoader(repo).GetCommitHistory(pathToRepo, committish));
        }

        public void Save()
        {
            foreach (var p in _commitLoaders.Values)
            {
                p.Save();
            }
        }

        public void Dispose()
        {
            foreach (var provider in _commitLoaders.Values)
            {
                provider.Dispose();
            }
        }

        private Repository? GetRepositoryCore(string directory)
        {
            var repoPath = GitUtility.FindRepository(directory);
            if (repoPath is null)
            {
                return null;
            }

            if (repoPath == Repository?.Path)
            {
                return Repository;
            }

            return Repository.Create(repoPath, branch: null);
        }

        private GitCommitLoader GetCommitLoader(Repository repo)
        {
            return _commitLoaders.GetOrAdd(
                repo.Path,
                _ => new GitCommitLoader(_errors, repo, AppData.GetCommitCachePath(repo.Url)));
        }
    }
}
