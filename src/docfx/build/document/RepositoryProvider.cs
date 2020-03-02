// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly string _docsetPath;
        private readonly Repository _repository;
        private readonly string _locale;
        private readonly PackageResolver? _packageResolver;
        private readonly Config? _config;
        private readonly LocalizationProvider? _localizationProvider;

        private readonly Lazy<(string path, Repository?)> _templateRepository;
        private readonly ConcurrentDictionary<PathString, Lazy<(string docset, Repository? repository)>> _dependencyRepositories
                   = new ConcurrentDictionary<PathString, Lazy<(string docset, Repository? repository)>>();

        public RepositoryProvider(
            string docsetPath,
            Repository repository,
            Config? config = null,
            PackageResolver? packageResolver = null,
            LocalizationProvider? localizationProvider = null)
        {
            _docsetPath = docsetPath;
            _repository = repository;
            _packageResolver = packageResolver;
            _locale = LocalizationUtility.GetLocale(repository);
            _config = config;
            _localizationProvider = localizationProvider;
            _templateRepository = new Lazy<(string, Repository?)>(GetTemplateRepository);
        }

        public Repository? GetRepository(FileOrigin origin, PathString? dependencyName = null)
        {
            return GetRepositoryWithDocsetEntry(origin, dependencyName).repository;
        }

        public (string docsetPath, Repository? repository) GetRepositoryWithDocsetEntry(FileOrigin origin, PathString? dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Redirection:
                case FileOrigin.Default:
                    return (_docsetPath, _repository);

                case FileOrigin.Fallback when _localizationProvider != null:
                    return _localizationProvider.GetFallbackRepositoryWithDocsetEntry();

                case FileOrigin.Template when _config != null && _packageResolver != null:
                    return _templateRepository.Value;

                case FileOrigin.Dependency when _config != null && _packageResolver != null && dependencyName != null:
                    return _dependencyRepositories.GetOrAdd(dependencyName.Value, _ => new Lazy<(string docset, Repository? repository)>(() =>
                    {
                        var dependency = _config.Dependencies[dependencyName.Value];
                        var dependencyPath = _packageResolver.ResolvePackage(dependency, dependency.PackageFetchOptions);

                        if (dependency.Type != PackageType.Git)
                        {
                            // point to a folder
                            return (dependencyPath, null);
                        }

                        return (dependencyPath, Repository.Create(dependencyPath, dependency.Branch, dependency.Url));
                    })).Value;
            }

            throw new InvalidOperationException();
        }

        private (string path, Repository?) GetTemplateRepository()
        {
            if (_config is null || _packageResolver is null)
            {
                throw new InvalidOperationException();
            }

            var theme = LocalizationUtility.GetLocalizedTheme(_config.Template, _locale, _config.DefaultLocale);

            var templatePath = _packageResolver.ResolvePackage(theme, PackageFetchOptions.DepthOne);

            if (theme.Type != PackageType.Git)
            {
                // point to a folder
                return (templatePath, null);
            }

            return (templatePath, Repository.Create(templatePath, theme.Branch, theme.Url));
        }
    }
}
