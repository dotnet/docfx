// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly string _docsetPath;
        private readonly Func<RestoreGitMap> _restoreMap;
        private readonly Func<Config> _config;
        private readonly Repository _docsetRepository;
        private readonly Lazy<Repository> _fallbackRepository;

        private readonly ConcurrentDictionary<string, Repository> _repositories = new ConcurrentDictionary<string, Repository>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, Repository> _dependencyRepositories = new ConcurrentDictionary<string, Repository>(PathUtility.PathComparer);

        public RepositoryProvider(string docsetPath, Func<Config> config, Func<RestoreGitMap> restoreMap)
        {
            _restoreMap = restoreMap;
            _config = config;
            _docsetPath = docsetPath;
            _docsetRepository = GetRepository(docsetPath);
            _fallbackRepository = new Lazy<Repository>(GetFallbackRepository);
        }

        public (Repository repo, string pathToRepo) GetRepository(FilePath file)
        {
            switch (file.Origin)
            {
                case FileOrigin.Fallback:
                case FileOrigin.Default:
                    var fullPath = Path.Combine(_docsetPath, file.Path).Replace('\\', '/');
                    var repo = GetRepository(fullPath);
                    var pathToRepo = Path.GetRelativePath(repo.Path, fullPath).Replace('\\', '/');
                    return (file.Origin == FileOrigin.Default ? repo : _fallbackRepository.Value, pathToRepo);

                case FileOrigin.Dependency:
                    return (GetRepository(file.Origin, file.DependencyName), file.GetPathToOrigin());

                default:
                    throw new NotSupportedException();
            }
        }

        public Repository GetRepository(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Default:
                    return _docsetRepository;

                case FileOrigin.Fallback:
                    return _fallbackRepository.Value;

                case FileOrigin.Template:
                    {
                        // localized template
                        var config = _config() ?? throw new InvalidOperationException();
                        return GetRepository(config.Template, bare: false);
                    }

                case FileOrigin.Dependency when _config != null && _restoreMap != null && dependencyName != null:
                    return _dependencyRepositories.GetOrAdd(dependencyName, _ =>
                    {
                        var config = _config() ?? throw new InvalidOperationException();
                        return GetRepository(config.Dependencies[dependencyName], bare: true);
                    });
            }

            throw new NotSupportedException();
        }

        public Repository GetRepository(string url, string branch, bool bare)
        {
            var restoreMap = _restoreMap() ?? throw new InvalidOperationException();
            var (path, commit) = restoreMap.GetRestoreGitPath(url, branch, bare);

            return Repository.Create(path, branch, url, commit);
        }

        private Repository GetRepository(PackagePath packagePath, bool bare)
        {
            switch (packagePath.Type)
            {
                case PackageType.Git:
                    return GetRepository(packagePath.Url, packagePath.Branch, bare);

                default:
                    // TODO: can also lookup repository for folder
                    return null;
            }
        }

        private Repository GetFallbackRepository()
        {
            if (LocalizationUtility.TryGetFallbackRepository(_docsetRepository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                var restoreMap = _restoreMap() ?? throw new InvalidOperationException();

                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (path, commit) = restoreMap.GetRestoreGitPath(fallbackRemote, branch, bare: false);
                        return Repository.Create(path, branch, fallbackRemote, commit);
                    }
                }
            }

            return default;
        }

        private Repository GetRepository(string fullPath)
        {
            if (GitUtility.IsRepo(fullPath))
            {
                return Repository.Create(fullPath, EnvironmentVariable.RepositoryBranch, EnvironmentVariable.RepositoryUrl);
            }

            var parent = PathUtility.NormalizeFile(Path.GetDirectoryName(fullPath) ?? "");
            return !string.IsNullOrEmpty(parent)
                ? _repositories.GetOrAdd(parent, GetRepository)
                : null;
        }
    }
}
