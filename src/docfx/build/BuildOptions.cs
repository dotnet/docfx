// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Represents build options needed before loading config (excluding preload config)
    /// </summary>
    internal class BuildOptions
    {
        public PathString DocsetPath { get; }

        public PathString? FallbackDocsetPath { get; }

        public PathString OutputPath { get; }

        public Repository? Repository { get; }

        /// <summary>
        /// Gets the lower-case culture name computed from <see cref="CommandLineOptions.Locale" or <see cref="Config.DefaultLocale"/>/>
        /// </summary>
        public string Locale { get; }

        public CultureInfo Culture { get; }

        public bool IsLocalizedBuild => FallbackDocsetPath != null;

        public bool EnableSideBySide { get; }

        public BuildOptions(string docsetPath, string? fallbackDocsetPath, string? outputPath, Repository? repository, PreloadConfig config)
        {
            Repository = repository;
            DocsetPath = new PathString(Path.GetFullPath(docsetPath));
            if (fallbackDocsetPath != null)
            {
                FallbackDocsetPath = new PathString(Path.GetFullPath(fallbackDocsetPath));
            }
            OutputPath = new PathString(Path.GetFullPath(outputPath ?? Path.Combine(docsetPath, config.OutputPath)));
            Locale = (LocalizationUtility.GetLocale(repository) ?? config.DefaultLocale).ToLowerInvariant();
            Culture = CreateCultureInfo(Locale);

            if (repository != null && !string.Equals(Locale, config.DefaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                EnableSideBySide =
                    LocalizationUtility.TryGetContributionBranch(repository.Branch, out var contributionBranch) &&
                    contributionBranch != repository.Branch;
            }
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
