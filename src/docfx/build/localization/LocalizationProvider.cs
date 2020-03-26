// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class LocalizationProvider
    {
        /// <summary>
        /// Gets the lower-case culture name computed from <see cref="CommandLineOptions.Locale" or <see cref="Config.DefaultLocale"/>/>
        /// </summary>
        public string Locale { get; }

        public CultureInfo Culture { get; }

        public PathString? FallbackDocsetPath { get; }

        public bool EnableSideBySide { get; }

        public LocalizationProvider(PackageResolver packageResolver, Config config, string? locale, string docsetPath, Repository? repository)
        {
            Locale = !string.IsNullOrEmpty(locale) ? locale.ToLowerInvariant() : config.DefaultLocale;
            Culture = CreateCultureInfo(Locale);

            if (repository != null)
            {
                EnableSideBySide =
                    LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch) &&
                    contributionBranch != repository.Branch;

                FallbackDocsetPath = GetFallbackDocsetPath(docsetPath, repository, packageResolver);
            }
        }

        public Docset? GetFallbackDocset()
        {
            return FallbackDocsetPath != null ? new Docset(FallbackDocsetPath) : null;
        }

        private static PathString? GetFallbackDocsetPath(string docsetPath, Repository repository, PackageResolver packageResolver)
        {
            var docsetSourceFolder = Path.GetRelativePath(repository.Path, docsetPath);
            if (LocalizationUtility.TryGetFallbackRepository(repository?.Remote, repository?.Branch, out var fallbackRemote, out var fallbackBranch))
            {
                foreach (var branch in new[] { fallbackBranch, "master" })
                {
                    if (packageResolver.TryResolvePackage(
                        new PackagePath(fallbackRemote, branch), PackageFetchOptions.None, out var fallbackRepoPath))
                    {
                        return new PathString(Path.Combine(fallbackRepoPath, docsetSourceFolder));
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
