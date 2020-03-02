// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class LocalizationUtility
    {
        private static readonly HashSet<string> s_locales = new HashSet<string>(
            CultureInfo.GetCultures(CultureTypes.AllCultures).Except(
                CultureInfo.GetCultures(CultureTypes.NeutralCultures)).Select(c => c.Name).Concat(
                    new[] { "zh-cn", "zh-tw", "zh-hk", "zh-sg", "zh-mo" }),
            StringComparer.OrdinalIgnoreCase);

        private static readonly Regex s_nameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?|\.loc)?$", RegexOptions.IgnoreCase);
        private static readonly Regex s_lrmAdjustment = new Regex(@"(^|\s|\>)(C#|F#|C\+\+)(\s*|[.!?;:]*)(\<|[\n\r]|$)", RegexOptions.IgnoreCase);

        public static bool IsValidLocale(string locale) => s_locales.Contains(locale);

        public static string AddLeftToRightMarker(CultureInfo culture, string text)
        {
            if (!culture.TextInfo.IsRightToLeft)
            {
                return text;
            }

            // This is used to protect against C#, F# and C++ from being split up when they are at the end of line of RTL text.
            // Find a(space or >), followed by product name, followed by zero or more(spaces or punctuation), followed by a(&lt; or newline)
            // &lrm is added after name to prevent the punctuation from moving to the other end of the line.
            // This should only be run on strings that are marked as RTL
            // & lrm may be added at places other than the end of a string, and that is ok
            return s_lrmAdjustment.Replace(text, me => $"{me.Groups[1]}{me.Groups[2]}&lrm;{me.Groups[3]}{me.Groups[4]}");
        }

        public static (string remote, string branch) GetLocalizedRepo(bool bilingual, string remote, string branch, string locale, string defaultLocale)
        {
            var newRemote = GetLocalizationName(remote, locale, defaultLocale);
            var newBranch = bilingual ? GetBilingualBranch(branch) : branch;

            return (newRemote, newBranch);
        }

        public static bool TryGetFallbackRepository(
            string? remote,
            string? branch,
            [NotNullWhen(true)] out string? fallbackRemote,
            [NotNullWhen(true)] out string? fallbackBranch)
        {
            fallbackRemote = null;
            fallbackBranch = null;

            if (string.IsNullOrEmpty(remote) || string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(remote, out fallbackRemote, out _))
            {
                fallbackBranch = branch;
                if (TryRemoveLocale(branch, out var branchWithoutLocale, out _))
                {
                    fallbackBranch = branchWithoutLocale;
                }

                if (TryGetContributionBranch(fallbackBranch, out var contributionBranch))
                {
                    fallbackBranch = contributionBranch;
                }

                return true;
            }

            return false;
        }

        public static string GetLocale(Repository? repository)
        {
            return TryRemoveLocale(repository?.Remote, out _, out var remoteLocale)
                ? remoteLocale : "en-us";
        }

        public static bool TryGetContributionBranch(string branch, [NotNullWhen(true)] out string? contributionBranch)
        {
            contributionBranch = null;
            if (string.IsNullOrEmpty(branch))
            {
                return false;
            }

            if (TryRemoveLocale(branch, out var branchWithoutLocale, out var locale))
            {
                branch = branchWithoutLocale;
            }

            if (branch.EndsWith("-sxs"))
            {
                contributionBranch = branch.Substring(0, branch.Length - 4);
                if (!string.IsNullOrEmpty(locale))
                {
                    contributionBranch += $".{locale}";
                }
                return true;
            }

            return false;
        }

        public static PackagePath GetLocalizedTheme(PackagePath theme, string locale, string defaultLocale)
        {
            switch (theme.Type)
            {
                case PackageType.Folder:
                    return new PackagePath(
                        GetLocalizationName(theme.Path, locale, defaultLocale));

                case PackageType.Git:
                    return new PackagePath(
                        GetLocalizationName(theme.Url, locale, defaultLocale),
                        theme.Branch);

                default:
                    return theme;
            }
        }

        private static bool TryRemoveLocale(string? name, [NotNullWhen(true)] out string? nameWithoutLocale, [NotNullWhen(true)] out string? locale)
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
                nameWithoutLocale = name.Substring(0, name.Length - match.Groups[1].Value.Length).ToLowerInvariant();
                return true;
            }

            return false;
        }

        private static string GetBilingualBranch(string branch)
        {
            return string.IsNullOrEmpty(branch) ? branch : $"{branch}-sxs";
        }

        private static string GetLocalizationName(string name, string locale, string defaultLocale)
        {
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

            var newLocale = $".{locale}";
            if (name.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return name;
            }

            return $"{name}{newLocale}";
        }
    }
}
