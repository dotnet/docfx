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
        private static readonly Regex s_repoNameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?|\.localization)?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The loc repo name follows below conventions:
        /// source remote                                           -->     loc remote
        /// https:://github.com/{org}/{repo-name}                   -->     https:://github.com/{org}/{repo-name}.{locale}
        /// https:://github.com/{org}/{repo-name}.{source-locale}   -->     https:://github.com/{org}/{repo-name}.{loc-locale}
        /// // TODO: org name can be different
        /// </summary>
        /// <returns>The loc remote url</returns>
        public static string GetLocalizationRepo(LocalizationMapping localizationMapping, string remote, string locale, string defaultLocale)
        {
            if (localizationMapping != LocalizationMapping.Repository && localizationMapping != LocalizationMapping.RepositoryAndFolder)
            {
                return remote;
            }

            if (string.Equals(locale, defaultLocale, StringComparison.OrdinalIgnoreCase))
            {
                return remote;
            }

            if (string.IsNullOrEmpty(remote))
            {
                return remote;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return remote;
            }

            var newLocale = localizationMapping == LocalizationMapping.Repository ? $".{locale}" : ".localization";
            var repoName = remote.Split(new char[] { '/', '\\' }).Last();
            var match = s_repoNameWithLocale.Match(repoName);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var originLocale = match.Groups[1].Value;
                return remote.Replace(originLocale, newLocale);
            }

            return $"{remote}{newLocale}";
        }

        public static string GetLocalizationDocsetPath(string docsetPath, Config config, string locale, RestoreMap restoreMap)
        {
            Debug.Assert(!string.IsNullOrEmpty(docsetPath));
            Debug.Assert(!string.IsNullOrEmpty(locale));
            Debug.Assert(config != null);
            Debug.Assert(restoreMap != null);

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
                        var locRemote = GetLocalizationRepo(config.Localization.Mapping, repo.Remote, locale, config.DefaultLocale);
                        var restorePath = restoreMap.GetGitRestorePath($"{locRemote}#{repo.Branch}");
                        localizationDocsetPath = config.Localization.Mapping == LocalizationMapping.Repository
                            ? restorePath
                            : Path.Combine(restorePath, locale);
                        break;
                    }
                case LocalizationMapping.Folder:
                    {
                        localizationDocsetPath = Path.Combine(localizationDocsetPath, "localization", locale);
                        break;
                    }
                default:
                    throw new NotSupportedException($"{config.Localization.Mapping} is not supported yet");
            }

            return localizationDocsetPath;
        }
    }
}
