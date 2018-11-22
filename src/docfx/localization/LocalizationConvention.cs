// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationConvention
    {
        /// <summary>
        /// The loc repo name follows below conventions:
        /// source remote                                           -->     loc remote
        /// https:://github.com/{org}/{repo-name}                   -->     https:://github.com/{org}/{repo-name}.{locale}
        /// https:://github.com/{org}/{repo-name}.{source-locale}   -->     https:://github.com/{org}/{repo-name}.{loc-locale}
        /// // TODO: org name can be different
        /// </summary>
        /// <returns>The loc remote url</returns>
        public static (string remote, string branch) GetLocalizationRepo(LocalizationMapping mapping, bool bilingual, string remote, string branch, string locale, string defaultLocale)
        {
            if (mapping != LocalizationMapping.Repository && mapping != LocalizationMapping.RepositoryAndFolder)
            {
                return (remote, branch);
            }

            if (string.Equals(locale, defaultLocale))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(remote))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(branch))
            {
                return (remote, branch);
            }

            if (string.IsNullOrEmpty(locale))
            {
                return (remote, branch);
            }

            var newLocale = mapping == LocalizationMapping.Repository ? $".{locale}" : ".localization";
            var newBranch = bilingual ? $"{branch}-sxs" : branch;

            if (remote.EndsWith($".{defaultLocale}", StringComparison.OrdinalIgnoreCase))
            {
                remote = remote.Substring(0, remote.Length - $".{defaultLocale}".Length);
            }

            if (remote.EndsWith(newLocale, StringComparison.OrdinalIgnoreCase))
            {
                return (remote, newBranch);
            }

            return ($"{remote}{newLocale}", newBranch);
        }

        public static string GetLocalizationDocsetPath(string docsetPath, Config config, string locale)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);

            var localizationDocsetPath = docsetPath;
            switch (config.Localization.Mapping)
            {
                case LocalizationMapping.Repository:
                case LocalizationMapping.RepositoryAndFolder:
                    {
                        var repo = Repository.CreateFromFolder(Path.GetFullPath(docsetPath));
                        if (repo == null)
                        {
                            return null;
                        }
                        var (locRemote, locBranch) = GetLocalizationRepo(
                            config.Localization.Mapping,
                            config.Localization.Bilingual,
                            repo.Remote,
                            repo.Branch,
                            locale,
                            config.Localization.DefaultLocale);
                        var restorePath = RestoreMap.GetGitRestorePath(locRemote, locBranch);
                        localizationDocsetPath = config.Localization.Mapping == LocalizationMapping.Repository
                            ? restorePath
                            : Path.Combine(restorePath, locale);
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        if (config.Localization.Bilingual)
                        {
                            throw new NotSupportedException($"{config.Localization.Mapping} is not supporting bilingual build");
                        }
                        localizationDocsetPath = Path.Combine(localizationDocsetPath, "localization", locale);
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return localizationDocsetPath;
        }

        public static (string remote, string branch) GetLocalizationTheme(string theme, string locale, string defaultLocale)
        {
            Debug.Assert(!string.IsNullOrEmpty(theme));
            var (remote, branch) = HrefUtility.SplitGitHref(theme);

            if (string.IsNullOrEmpty(locale))
            {
                return (remote, branch);
            }

            if (string.Equals(locale, defaultLocale))
            {
                return (remote, branch);
            }

            if (remote.EndsWith($".{defaultLocale}", StringComparison.OrdinalIgnoreCase))
            {
                remote = remote.Substring(0, remote.Length - $".{defaultLocale}".Length);
            }

            if (remote.EndsWith($".{locale}", StringComparison.OrdinalIgnoreCase))
            {
                return (remote, branch);
            }

            return ($"{remote}.{locale}", branch);
        }
    }
}
