// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class RepositoryProvider
    {
        private readonly ConcurrentDictionary<string, Lazy<(string docset, Repository repository)>> _repositories = new ConcurrentDictionary<string, Lazy<(string docset, Repository repository)>>();

        private string _locale;
        private string _docset;
        private string _fallbackDocset;
        private Repository _docsetRepository;
        private Repository _fallbackRepository;
        private RestoreGitMap _restoreGitMap;
        private Config _config;

        public RepositoryProvider(string docsetPath, CommandLineOptions options)
        {
            _docset = docsetPath;
            _docsetRepository = Repository.Create(docsetPath);
            _locale = LocalizationUtility.GetLocale(_docsetRepository, options);

            _fallbackDocset = default;
            _fallbackRepository = default;
            _restoreGitMap = default;
            _config = default;
        }

        public void ConfigFallbackRepository(Repository fallbackRepository)
        {
            Debug.Assert(_fallbackRepository is null);

            _fallbackDocset = fallbackRepository.Path;
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

            _restoreGitMap = restoreGitMap;
            _fallbackRepository = GetFallbackRepository(_docsetRepository, restoreGitMap);
            _fallbackDocset = _fallbackRepository?.Path;
        }

        public void ConfigLocalizationRepo(string localizationDocsetPath, Repository localizationRepository)
        {
            Debug.Assert(_fallbackRepository is null);

            _fallbackDocset = _docset;
            _fallbackRepository = _docsetRepository;
            _docset = localizationDocsetPath;
            _docsetRepository = localizationRepository;
        }

        public Repository GetRepository(FileOrigin origin, string dependencyName = null)
        {
            return GetRepositoryWithEntry(origin, dependencyName).repository;
        }

        public (string entry, Repository repository) GetRepositoryWithEntry(FileOrigin origin, string dependencyName = null)
        {
            switch (origin)
            {
                case FileOrigin.Redirection:
                case FileOrigin.Default:
                    return (_docset, _docsetRepository);

                case FileOrigin.Fallback:
                    return (_fallbackDocset, _fallbackRepository);

                case FileOrigin.Template when _config != null && _restoreGitMap != null:
                    return _repositories.GetOrAdd(origin.ToString(), _ =>
                    new Lazy<(string docset, Repository repository)>(() =>
                    {
                        var theme = LocalizationUtility.GetLocalizedTheme(_config.Template, _locale, _config.Localization.DefaultLocale);

                        var (templatePath, templateCommit) = _restoreGitMap.GetRestoreGitPath(theme, false);

                        if (theme.Type != PackageType.Git)
                        {
                            // point to a folder
                            return (templatePath, null);
                        }

                        return (templatePath, Repository.Create(templatePath, _config.Template.Branch, _config.Template.Url, templateCommit));
                    })).Value;

                case FileOrigin.Dependency when _config != null && _restoreGitMap != null && dependencyName != null:
                    return _repositories.GetOrAdd(origin.ToString() + dependencyName, _ =>
                    new Lazy<(string docset, Repository repository)>(() =>
                    {
                        var (dependencyPath, dependencyCommit) = _restoreGitMap.GetRestoreGitPath(_config.Dependencies[dependencyName], bare: true);

                        if (_config.Dependencies[dependencyName].Type != PackageType.Git)
                        {
                            // point to a folder
                            return (dependencyPath, null);
                        }

                        return (dependencyPath, Repository.Create(dependencyPath, _config.Template.Branch, _config.Template.Url, dependencyCommit));
                    })).Value;
            }

            return default;
        }

        private static Repository GetFallbackRepository(
            Repository repository,
            RestoreGitMap restoreGitMap)
        {
            Debug.Assert(restoreGitMap != null);

            if (LocalizationUtility.TryGetFallbackRepository(repository, out var fallbackRemote, out var fallbackBranch, out _))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (restoreGitMap.IsBranchRestored(fallbackRemote, branch))
                    {
                        var (fallbackRepoPath, fallbackRepoCommit) = restoreGitMap.GetRestoreGitPath(new PackageUrl(fallbackRemote, branch), bare: false);
                        return Repository.Create(fallbackRepoPath, branch, fallbackRemote, fallbackRepoCommit);
                    }
                }
            }

            return default;
        }
    }
}
