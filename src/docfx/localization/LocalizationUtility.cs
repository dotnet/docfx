// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationUtility
    {
        private static readonly Regex s_nameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?|\.loc)?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The loc repo remote and branch based on localization mapping<see cref="LocalizationMapping"/>
        /// </summary>
        public static (string remote, string branch) GetLocalizedRepo(LocalizationMapping mapping, bool bilingual, string remote, string branch, string locale, string defaultLocale)
        {
            var newRemote = GetLocalizationName(mapping, remote, locale, defaultLocale);
            var newBranch = bilingual
                ? GetLocalizationBranch(mapping, GetBilingualBranch(mapping, branch), locale, defaultLocale)
                : GetLocalizationBranch(mapping, branch, locale, defaultLocale);

            return (newRemote, newBranch);
        }

        public static bool TryGetLocalizedDocsetPath(Docset docset, Config config, string locale, out string localizationDocsetPath, out string localizationBranch, out DependencyLockModel subDependencyLock)
        {
            Debug.Assert(docset != null);
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);

            localizationDocsetPath = null;
            localizationBranch = null;
            subDependencyLock = null;
            switch (config.Localization.Mapping)
            {
                case LocalizationMapping.Repository:
                case LocalizationMapping.Branch:
                    {
                        var repo = docset.Repository;
                        if (repo == null)
                        {
                            return false;
                        }
                        var (locRemote, locBranch) = GetLocalizedRepo(
                            config.Localization.Mapping,
                            config.Localization.Bilingual,
                            repo.Remote,
                            repo.Branch,
                            locale,
                            config.Localization.DefaultLocale);
                        (localizationDocsetPath, subDependencyLock) = RestoreMap.GetGitRestorePath(locRemote, locBranch, docset.DependencyLock);
                        localizationBranch = locBranch;
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        if (config.Localization.Bilingual)
                        {
                            throw new NotSupportedException($"{config.Localization.Mapping} is not supporting bilingual build");
                        }
                        localizationDocsetPath = Path.Combine(docset.DocsetPath, "localization", locale);
                        localizationBranch = null;
                        subDependencyLock = null;
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return true;
        }

        public static bool TryGetSourceRepository(Repository repository, out string sourceRemote, out string sourceBranch, out string locale)
        {
            sourceRemote = null;
            sourceBranch = null;
            locale = null;

            if (repository == null || string.IsNullOrEmpty(repository.Remote))
            {
                return false;
            }

            return TryGetSourceRepository(repository.Remote, repository.Branch, out sourceRemote, out sourceBranch, out locale);
        }

        /// <summary>
        /// Get the source repo's remote and branch from loc repo based on <see cref="LocalizationMapping"/>
        /// </summary>
        public static bool TryGetSourceRepository(string remote, string branch, out string sourceRemote, out string sourceBranch, out string locale)
        {
            sourceRemote = null;
            sourceBranch = null;
            locale = null;

            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(remote, out sourceRemote, out locale))
            {
                sourceBranch = branch;
                if (TryRemoveLocale(branch, out var branchWithoutLocale, out var branchLocale))
                {
                    sourceBranch = branchWithoutLocale;
                    locale = branchLocale;
                }

                if (TryGetContributionBranch(sourceBranch, out var contributionBranch))
                {
                    sourceBranch = contributionBranch;
                }
                return true;
            }

            return locale != null;
        }

        public static bool TryGetSourceDocsetPath(Docset docset, out string sourceDocsetPath, out string sourceBranch, out DependencyLockModel dependencyLock)
        {
            sourceDocsetPath = null;
            sourceBranch = null;
            dependencyLock = null;

            Debug.Assert(docset != null);

            if (TryGetSourceRepository(docset.Repository, out var sourceRemote, out sourceBranch, out var locale))
            {
                (sourceDocsetPath, dependencyLock) = RestoreMap.GetGitRestorePath(sourceRemote, sourceBranch, docset.DependencyLock);
                return true;
            }

            return false;
        }

        public static bool TryGetContributionBranch(Repository repository, out string contributionBranch)
        {
            contributionBranch = null;

            if (repository == null)
            {
                return false;
            }

            return TryGetContributionBranch(repository.Branch, out contributionBranch);
        }

        public static bool TryGetContributionBranch(string branch, out string contributionBranch)
        {
            contributionBranch = null;
            string locale = null;
            if (string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(branch, out var branchWithoutLocale, out locale))
            {
                branch = branchWithoutLocale;
            }

            if (branch.EndsWith("-sxs"))
            {
                contributionBranch = branch.Substring(0, branch.Length - 4);
                if (!string.IsNullOrEmpty(locale))
                {
                    contributionBranch = contributionBranch + $".{locale}";
                }
                return true;
            }

            return false;
        }

        public static string GetLocale(Repository repository, CommandLineOptions options)
        {
            return TryGetSourceRepository(repository, out _, out _, out var locale) ? locale : options.Locale;
        }

        public static bool IsLocalized(this Docset docset) => docset.FallbackDocset != null;

        public static bool IsLocalizedBuild(this Docset docset) => docset.FallbackDocset != null || docset.LocalizationDocset != null;

        public static (string remote, string branch) GetLocalizedTheme(string theme, string locale, string defaultLocale)
        {
            Debug.Assert(!string.IsNullOrEmpty(theme));
            var (remote, branch) = HrefUtility.SplitGitHref(theme);

            return (GetLocalizationName(LocalizationMapping.Repository, remote, locale, defaultLocale), branch);
        }

        public static bool TryRemoveLocale(string name, out string nameWithoutLocale, out string locale)
        {
            nameWithoutLocale = null;
            locale = null;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var match = s_nameWithLocale.Match(name);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                locale = match.Groups[1].Value.Substring(1).ToLowerInvariant();
                nameWithoutLocale = name.Substring(0, name.Length - match.Groups[1].Value.Length);

                return true;
            }

            return false;
        }

        private static string GetBilingualBranch(LocalizationMapping mapping, string branch)
        {
            if (mapping == LocalizationMapping.Folder)
            {
                return branch;
            }

            return string.IsNullOrEmpty(branch) ? branch : $"{branch}-sxs";
        }

        private static string GetLocalizationBranch(LocalizationMapping mapping, string sourceBranch, string locale, string defaultLocale)
        {
            if (mapping != LocalizationMapping.Branch)
            {
                return sourceBranch;
            }

            if (string.IsNullOrEmpty(sourceBranch))
            {
                return sourceBranch;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return sourceBranch;
            }

            if (string.Equals(locale, defaultLocale))
            {
                return sourceBranch;
            }

            return $"{sourceBranch}.{locale}";
        }

        private static string GetLocalizationName(LocalizationMapping mapping, string name, string locale, string defaultLocale)
        {
            if (mapping == LocalizationMapping.Folder)
            {
                return name;
            }

            if (string.Equals(locale, defaultLocale))
            {
                return name;
            }

            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return name;
            }

            var newLocale = mapping == LocalizationMapping.Repository ? $".{locale}" : ".loc";
            if (name.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            return $"{name}{newLocale}";
        }
    }
}
