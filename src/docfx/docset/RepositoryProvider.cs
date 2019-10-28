// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly ConcurrentDictionary<(FileOrigin origin, string dependencyName), Lazy<(string docset, Repository repository)>> _dependencyRepositories
            = new ConcurrentDictionary<(FileOrigin origin, string dependencyName), Lazy<(string docset, Repository repository)>>();

        private readonly string _locale;
        private string _docsetPath;
        private string _fallbackDocsetPath;
        private Repository _docsetRepository;
        private Repository _fallbackRepository;
        private RestoreGitMap _restoreGitMap;
        private Config _config;

        public RepositoryProvider(string docsetPath, CommandLineOptions options)
        {
            _docsetPath = docsetPath;
            _docsetRepository = Repository.Create(docsetPath);
            _locale = LocalizationUtility.GetLocale(_docsetRepository, options);

            _fallbackRepository = default;
            _restoreGitMap = default;
            _config = default;
        }

        public void ConfigFallbackRepository(Repository fallbackRepository)
        {
            Debug.Assert(_fallbackRepository is null);

            _fallbackRepository = fallbackRepository;
        }

        public void Config(Config config)
        {
            Debug.Assert(_config is null);

            _config = config;
        }

        public void ConfigRestoreMap(RestoreGitMap restoreGitMap)
        {
            Debug.Assert(_fallbackRepository is null);
            Debug.Assert(_restoreGitMap is null);
            Debug.Assert(restoreGitMap != null);

            _restoreGitMap = restoreGitMap;
            _fallbackRepository = GetFallbackRepository(_docsetRepository, restoreGitMap);
        }

        public void ConfigLocalizationRepo(string localizationDocsetPath, Repository localizationRepository)
        {
            Debug.Assert(_fallbackRepository is null);

            _fallbackRepository = _docsetRepository;
            _docsetPath = localizationDocsetPath;
            _docsetRepository = localizationRepository;
        }

        public void ConfigFallbackDocsetPath(string fallbackDocsetPath)
        {
            _fallbackDocsetPath = fallbackDocsetPath;
        }

        public Repository GetRepository(FileOrigin origin, string dependencyName = null)
        {
            return GetRepositoryWithDocsetEntry(origin, dependencyName).repository;
        }

        public (string docsetPath, Repository repository) GetRepositoryWithDocsetEntry(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Redirection:
                case FileOrigin.Default:
                    return (_docsetPath, _docsetRepository);

                case FileOrigin.Fallback:
                    return (_fallbackDocsetPath, _fallbackRepository);

                case FileOrigin.Template when _config != null && _restoreGitMap != null:
                    return _dependencyRepositories.GetOrAdd((origin, dependencyName), _ =>
                    new Lazy<(string docset, Repository repository)>(() =>
                    {
                        var theme = LocalizationUtility.GetLocalizedTheme(_config.Template, _locale, _config.Localization.DefaultLocale);

                        var (templatePath, templateCommit) = _restoreGitMap.GetRestoreGitPath(theme, false);

                        if (theme.Type != PackageType.Git)
                        {
                            // point to a folder
                            return (templatePath, null);
                        }

                        return (templatePath, Repository.Create(templatePath, theme.Branch, theme.Url, templateCommit));
                    })).Value;

                case FileOrigin.Dependency when _config != null && _restoreGitMap != null && dependencyName != null:
                    return _dependencyRepositories.GetOrAdd((origin, dependencyName), _ =>
                    new Lazy<(string docset, Repository repository)>(() =>
                    {
                        var dependency = _config.Dependencies[dependencyName];
                        var (dependencyPath, dependencyCommit) = _restoreGitMap.GetRestoreGitPath(dependency, bare: true);

                        if (dependency.Type != PackageType.Git)
                        {
                            // point to a folder
                            return (dependencyPath, null);
                        }

                        return (dependencyPath, Repository.Create(dependencyPath, dependency.Branch, dependency.Url, dependencyCommit));
                    })).Value;
            }

            throw new InvalidOperationException();
        }

        private static Repository GetFallbackRepository(
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreGitMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (fallbackRepoPath, fallbackRepoCommit) = restoreGitMap.GetRestoreGitPath(new PackagePath(fallbackRemote, branch), bare: false);
                        return Repository.Create(fallbackRepoPath, branch, fallbackRemote, fallbackRepoCommit);
                    }
                }
            }

            return default;
        }
    }
}
