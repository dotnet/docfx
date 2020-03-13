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
        private readonly PathString? _docsetPathToDefaultRepository;
        private readonly PackageResolver? _packageResolver;
        private readonly Config? _config;
        private readonly LocalizationProvider? _localizationProvider;
        private readonly ConcurrentDictionary<string, Repository?> _repositores = new ConcurrentDictionary<string, Repository?>(PathUtility.PathComparer);

        private readonly ConcurrentDictionary<PathString, (string docset, Repository? repository)> _dependencyRepositories
                   = new ConcurrentDictionary<PathString, (string docset, Repository? repository)>();

        public Repository? DefaultRepository { get; }

        public RepositoryProvider(
            string docsetPath,
            Repository? repository,
            Config? config = null,
            PackageResolver? packageResolver = null,
            LocalizationProvider? localizationProvider = null)
        {
            _docsetPath = docsetPath;
            DefaultRepository = repository;
            _packageResolver = packageResolver;
            _config = config;
            _localizationProvider = localizationProvider;

            if (DefaultRepository != null)
            {
                _docsetPathToDefaultRepository = new PathString(Path.GetRelativePath(DefaultRepository.Path, _docsetPath));
            }
        }

        public Repository? GetRepository(FileOrigin origin, PathString? dependencyName = null)
        {
            return GetRepositoryWithDocsetEntry(origin, dependencyName).repository;
        }

        public (Repository? repository, PathString? pathToRepository) GetRepository(FilePath path)
        {
            return path.Origin switch
            {
                FileOrigin.Default => GetRepository(Path.Combine(_docsetPath, path.Path)),
                FileOrigin.Fallback when _localizationProvider != null
                    => (_localizationProvider.GetFallbackRepositoryWithDocsetEntry().fallbackRepository,
                        _docsetPathToDefaultRepository is null ? null : _docsetPathToDefaultRepository + path.Path),
                FileOrigin.Dependency => (GetRepositoryWithDocsetEntry(path.Origin, path.DependencyName).repository, path.GetPathToOrigin()),
                _ => throw new InvalidOperationException(),
            };
        }

        public (string docsetPath, Repository? repository) GetRepositoryWithDocsetEntry(FileOrigin origin, PathString? dependencyName = null)
        {
            return origin switch
            {
                FileOrigin.Default => (_docsetPath, DefaultRepository),
                FileOrigin.Fallback when _localizationProvider != null
                    => _localizationProvider.GetFallbackRepositoryWithDocsetEntry(),
                FileOrigin.Dependency when _config != null && _packageResolver != null && dependencyName != null
                    => _dependencyRepositories.GetOrAdd(dependencyName.Value, key => GetDependencyRepository(key, _config, _packageResolver)),
                _ => throw new InvalidOperationException(),
            };
        }

        private (Repository? repository, PathString? pathToRepository) GetRepository(string fullPath)
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (directory is null)
            {
                return default;
            }

            var repository = _repositores.GetOrAdd(directory, GetRepositoryCore);
            if (repository is null)
            {
                return default;
            }

            return (repository, new PathString(Path.GetRelativePath(repository.Path, fullPath)));
        }

        private Repository? GetRepositoryCore(string directory)
        {
            var repository = GitUtility.FindRepository(directory);
            if (repository is null)
            {
                return null;
            }

            if (string.Equals(repository, DefaultRepository?.Path))
            {
                return DefaultRepository;
            }

            return Repository.Create(repository);
        }

        private (string docset, Repository? repository) GetDependencyRepository(PathString dependencyName, Config config, PackageResolver packageResolver)
        {
            var dependency = config.Dependencies[dependencyName];
            var dependencyPath = packageResolver.ResolvePackage(dependency, dependency.PackageFetchOptions);

            if (dependency.Type != PackageType.Git)
            {
                // point to a folder
                return (dependencyPath, null);
            }

            return (dependencyPath, Repository.Create(dependencyPath, dependency.Branch, dependency.Url));
        }
    }
}
