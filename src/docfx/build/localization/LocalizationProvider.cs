// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalizationProvider
    {
        private readonly RestoreGitMap _restoreGitMap;
        private readonly string _locale;
        private readonly Config _config;
        private readonly bool _buildFromEnglish;
        private readonly string _entryDocsetPath;
        private readonly Repository _entryRepository;

        private string _englishDocsetPath;
        private Repository _englishRepository;
        private string _localizationDocsetPath;
        private Repository _localizationRepository;

        public bool IsLocalizationBuild { get; }

        public bool EnableSideBySide { get; }

        public LocalizationProvider(RestoreGitMap restoreGitMap, CommandLineOptions options, Config config, string locale, string docsetPath, Repository repository)
        {
            _restoreGitMap = restoreGitMap;
            _config = config;
            _locale = locale;
            _buildFromEnglish = !string.IsNullOrEmpty(options.Locale);
            _entryDocsetPath = docsetPath;
            _entryRepository = repository;

            if (!string.IsNullOrEmpty(locale) && !string.Equals(locale, config.Localization.DefaultLocale))
            {
                IsLocalizationBuild = true;
            }

            if (_buildFromEnglish)
            {
                _englishDocsetPath = docsetPath;
                _englishRepository = repository;

                EnableSideBySide = config.Localization.Bilingual;
            }
            else
            {
                _localizationDocsetPath = docsetPath;
                _localizationRepository = repository;

                EnableSideBySide = repository != null && repository.Branch.EndsWith("-sxs");
            }

            SetFallbackRepository();
        }

        public (string docsetPath, Repository repository) GetBuildRepositoryWithDocsetEntry()
        {
            return (_localizationDocsetPath, _localizationRepository);
        }

        public (string fallbackDocsetPath, Repository fallbackRepository) GetFallbackRepositoryWithDocsetEntry()
        {
            return (_englishDocsetPath, _englishRepository);
        }

        private void SetFallbackRepository()
        {
            if (_restoreGitMap is null || _config is null || _entryRepository is null)
            {
                return;
            }

            var docsetSourceFolder = Path.GetRelativePath(_entryRepository.Path, _entryDocsetPath);

            if (!_buildFromEnglish)
            {
                (_englishDocsetPath, _englishRepository) = _restoreGitMap != null ? GetFallbackRepository(_entryRepository, _restoreGitMap, docsetSourceFolder) : default;
            }
            else
            {
                if (TryGetLocalizationDocset(
                    _restoreGitMap,
                    _entryDocsetPath,
                    _entryRepository,
                    _config,
                    docsetSourceFolder,
                    _locale,
                    out var localizationDocsetPath,
                    out var localizationRepository))
                {
                    _localizationDocsetPath = localizationDocsetPath;
                    _localizationRepository = localizationRepository;
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

        private bool TryGetLocalizationDocset(RestoreGitMap restoreGitMap, string docsetPath, Repository docsetRepository, Config config, string docsetSourceFolder, string locale, out string localizationDocsetPath, out Repository localizationRepository)
        {
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);

            localizationDocsetPath = null;
            localizationRepository = null;
            switch (config.Localization.Mapping)
            {
                case LocalizationMapping.Repository:
                case LocalizationMapping.Branch:
                    {
                        var repo = docsetRepository;
                        if (repo is null)
                        {
                            return false;
                        }
                        var (locRemote, locBranch) = LocalizationUtility.GetLocalizedRepo(
                            config.Localization.Mapping,
                            EnableSideBySide,
                            repo.Remote,
                            repo.Branch,
                            locale,
                            config.Localization.DefaultLocale);
                        var (locRepoPath, locCommit) = restoreGitMap.GetRestoreGitPath(new PackagePath(locRemote, locBranch), false);
                        localizationDocsetPath = PathUtility.NormalizeFolder(Path.Combine(locRepoPath, docsetSourceFolder));
                        localizationRepository = Repository.Create(locRepoPath, locBranch, locRemote, locCommit);
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        if (config.Localization.Bilingual)
                        {
                            throw new NotSupportedException($"{config.Localization.Mapping} is not supporting bilingual build");
                        }
                        localizationDocsetPath = Path.Combine(docsetPath, "_localization", locale);
                        localizationRepository = docsetRepository;
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return true;
        }
    }
}
