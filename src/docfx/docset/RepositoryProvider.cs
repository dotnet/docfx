// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly ConcurrentDictionary<(FileOrigin origin, string dependencyName), Lazy<(string docset, Repository repository)>> _dependencyRepositories
            = new ConcurrentDictionary<(FileOrigin origin, string dependencyName), Lazy<(string docset, Repository repository)>>();

        private readonly string _locale;
        private readonly RestoreGitMap _restoreGitMap;

        private string _docsetPath;
        private Repository _docsetRepository;
        private string _fallbackDocsetPath;
        private Repository _fallbackRepository;
        private Config _config;

        public RepositoryProvider(string docsetPath, CommandLineOptions options, Repository docsetRepository, RestoreGitMap restoreGitMap = null)
        {
            _docsetPath = docsetPath;
            _docsetRepository = docsetRepository;
            _restoreGitMap = restoreGitMap;
            _locale = LocalizationUtility.GetLocale(_docsetRepository, options);
            _restoreGitMap = restoreGitMap;
            _config = default;
        }

        public void Config(Config config)
        {
            Debug.Assert(_config is null);

            _config = config;
            SetFallbackRepository();
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

        private void SetFallbackRepository()
        {
            if (_docsetRepository is null)
            {
                return;
            }

            var docsetSourceFolder = Path.GetRelativePath(_docsetRepository.Path, _docsetPath);
            (_fallbackDocsetPath, _fallbackRepository) = _restoreGitMap != null ? GetFallbackRepository(_docsetRepository, _restoreGitMap, docsetSourceFolder) : default;
            if (_fallbackRepository is null && _locale != null && !string.Equals(_locale, _config.Localization.DefaultLocale))
            {
                if (LocalizationUtility.TryGetLocalizationDocset(
                    _restoreGitMap,
                    _docsetPath,
                    _docsetRepository,
                    _config,
                    docsetSourceFolder,
                    _locale,
                    out var localizationDocsetPath,
                    out var localizationRepository))
                {
                    _fallbackDocsetPath = _docsetPath;
                    _fallbackRepository = _docsetRepository;
                    _docsetPath = localizationDocsetPath;
                    _docsetRepository = localizationRepository;
                }
            }
        }

        private static (string fallbackDocsetPath, Repository fallbackRepo) GetFallbackRepository(
            Repository repository,
            RestoreGitMap restoreGitMap,
            string docsetSourceFolder)
        {
            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreGitMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (fallbackRepoPath, fallbackRepoCommit) = restoreGitMap.GetRestoreGitPath(new PackagePath(fallbackRemote, branch), bare: false);
                        return (PathUtility.NormalizeFolder(Path.Combine(fallbackRepoPath, docsetSourceFolder)),
                            Repository.Create(fallbackRepoPath, branch, fallbackRemote, fallbackRepoCommit));
                    }
                }
            }

            return default;
        }
    }
}
