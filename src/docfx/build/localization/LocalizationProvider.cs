// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalizationProvider
    {
        // entry should always be localization repo
        private readonly string _localizationDocsetPath;
        private readonly Repository? _localizationRepository;

        // en-us repo is used for fallback
        private string? _englishDocsetPath;
        private Repository? _englishRepository;

        /// <summary>
        /// Gets the lower-case culture name computed from <see cref="CommandLineOptions.Locale" or <see cref="Config.DefaultLocale"/>/>
        /// </summary>
        public string Locale { get; }

        public CultureInfo Culture { get; }

        public bool IsLocalizationBuild { get; }

        public bool EnableSideBySide { get; }

        public LocalizationProvider(PackageResolver packageResolver, Config config, string? locale, string docsetPath, Repository? repository)
        {
            Locale = !string.IsNullOrEmpty(locale) ? locale.ToLowerInvariant() : config.DefaultLocale;
            Culture = CreateCultureInfo(Locale);

            _localizationDocsetPath = docsetPath;
            _localizationRepository = repository;

            if (!string.IsNullOrEmpty(locale) && !string.Equals(locale, config.DefaultLocale))
            {
                IsLocalizationBuild = true;
            }

            EnableSideBySide = repository != null &&
                LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch) &&
                contributionBranch != repository.Branch;

            if (_localizationRepository != null)
            {
                var docsetSourceFolder = Path.GetRelativePath(_localizationRepository.Path, _localizationDocsetPath);
                (_englishDocsetPath, _englishRepository) = GetFallbackRepository(_localizationRepository, packageResolver, docsetSourceFolder);
            }
        }

        public Docset? GetFallbackDocset()
        {
            return _englishDocsetPath != null ? new Docset(_englishDocsetPath) : null;
        }

        public (string fallbackDocsetPath, Repository? fallbackRepository) GetFallbackRepositoryWithDocsetEntry()
        {
            return (_englishDocsetPath ?? throw new InvalidOperationException(), _englishRepository);
        }

        private static (string fallbackDocsetPath, Repository? fallbackRepo) GetFallbackRepository(
            Repository? repository,
            PackageResolver packageResolver,
            string docsetSourceFolder)
        {
            if (LocalizationUtility.TryGetFallbackRepository(repository?.Remote, repository?.Branch, out var fallbackRemote, out var fallbackBranch))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (packageResolver.TryResolvePackage(
                        new PackagePath(fallbackRemote, branch), PackageFetchOptions.None, out var fallbackRepoPath))
                    {
                        return (PathUtility.NormalizeFolder(Path.Combine(fallbackRepoPath, docsetSourceFolder)),
                            Repository.Create(fallbackRepoPath, branch, fallbackRemote));
                    }
                }
            }

            return default;
        }

        private CultureInfo CreateCultureInfo(string locale)
        {
            try
            {
                return new CultureInfo(locale);
            }
            catch (CultureNotFoundException)
            {
                throw Errors.Config.LocaleInvalid(locale).ToException();
            }
        }
    }
}
