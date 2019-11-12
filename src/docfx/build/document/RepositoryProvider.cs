// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly string _locale;
        private readonly Func<RestoreGitMap> _restoreGitMap;
        private readonly Func<Config> _config;
        private readonly Lazy<(string fallbackDocsetPath, Repository fallbackRepo)> _fallbackDocset;

        private readonly Lazy<(string path, Repository)> _templateRepository;
        private readonly ConcurrentDictionary<PathString, Lazy<(string docset, Repository repository)>> _dependencyRepositories
                   = new ConcurrentDictionary<PathString, Lazy<(string docset, Repository repository)>>();

        private string _docsetPath;
        private Repository _docsetRepository;

        public RepositoryProvider(
            string docsetPath,
            CommandLineOptions options,
            Func<RestoreGitMap> restoreGitMap = null,
            Func<Config> config = null)
        {
            _docsetPath = docsetPath;
            _docsetRepository = Repository.Create(docsetPath);
            _restoreGitMap = restoreGitMap;
            _locale = LocalizationUtility.GetLocale(_docsetRepository, options);
            _restoreGitMap = restoreGitMap;
            _config = config;
            _fallbackDocset = new Lazy<(string, Repository)>(SetFallbackRepository);
            _templateRepository = new Lazy<(string, Repository)>(GetTemplateRepository);
        }

        public Repository GetRepository(FileOrigin origin, PathString? dependencyName = null)
        {
            return GetRepositoryWithDocsetEntry(origin, dependencyName).repository;
        }

        public (string docsetPath, Repository repository) GetRepositoryWithDocsetEntry(FileOrigin origin, PathString? dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Redirection:
                case FileOrigin.Default:
                    return (_docsetPath, _docsetRepository);

                case FileOrigin.Fallback:
                    return (_fallbackDocset.Value.fallbackDocsetPath, _fallbackDocset.Value.fallbackRepo);

                case FileOrigin.Template when _config != null && _restoreGitMap != null:
                    return _templateRepository.Value;

                case FileOrigin.Dependency when _config != null && _restoreGitMap != null && dependencyName != null:
                    return _dependencyRepositories.GetOrAdd(dependencyName.Value, _ => new Lazy<(string docset, Repository repository)>(() =>
                    {
                        var dependency = _config().Dependencies[dependencyName.Value];
                        var (dependencyPath, dependencyCommit) = _restoreGitMap().GetRestoreGitPath(dependency, bare: true);

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

        private (string fallbackDocsetPath, Repository fallbackRepo) SetFallbackRepository()
        {
            if (_restoreGitMap is null || _config is null)
            {
                return default;
            }

            Repository fallbackRepository;
            string fallbackDocsetPath;
            var docsetSourceFolder = Path.GetRelativePath(_docsetRepository.Path, _docsetPath);
            (fallbackDocsetPath, fallbackRepository) = _restoreGitMap != null ? GetFallbackRepository(_docsetRepository, _restoreGitMap(), docsetSourceFolder) : default;
            if (fallbackRepository is null && _locale != null && !string.Equals(_locale, _config().Localization.DefaultLocale))
            {
                if (LocalizationUtility.TryGetLocalizationDocset(
                    _restoreGitMap(),
                    _docsetPath,
                    _docsetRepository,
                    _config(),
                    docsetSourceFolder,
                    _locale,
                    out var localizationDocsetPath,
                    out var localizationRepository))
                {
                    fallbackDocsetPath = _docsetPath;
                    fallbackRepository = _docsetRepository;
                    _docsetPath = localizationDocsetPath;
                    _docsetRepository = localizationRepository;
                }
            }
            return (fallbackDocsetPath, fallbackRepository);
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

        private (string path, Repository) GetTemplateRepository()
        {
            var theme = LocalizationUtility.GetLocalizedTheme(_config().Template, _locale, _config().Localization.DefaultLocale);

            var (templatePath, templateCommit) = _restoreGitMap().GetRestoreGitPath(theme, false);

            if (theme.Type != PackageType.Git)
            {
                // point to a folder
                return (templatePath, null);
            }

            return (templatePath, Repository.Create(templatePath, theme.Branch, theme.Url, templateCommit));
        }
    }
}
