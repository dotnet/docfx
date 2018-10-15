// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocalizationConvention
    {
        private static readonly Regex s_repoNameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?)?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The loc repo name follows below conventions:
        /// source remote                                           -->     loc remote
        /// https:://github.com/{org}/{repo-name}                   -->     https:://github.com/{org}/{repo-name}.{locale}
        /// https:://github.com/{org}/{repo-name}.{source-locale}   -->     https:://github.com/{org}/{repo-name}.{loc-locale}
        /// // TODO: org name can be different
        /// </summary>
        /// <returns>The loc remote url</returns>
        public static (string remote, bool changed) GetLocalizationRepo(LocalizationMapping localizationMapping, string remote, string locale, string defaultLocale)
        {
            if (localizationMapping != LocalizationMapping.Repository && localizationMapping != LocalizationMapping.RepositoryAndFolder)
            {
                return (remote, false);
            }

            if (string.Equals(locale, defaultLocale, System.StringComparison.OrdinalIgnoreCase))
            {
                return (remote, false);
            }

            if (string.IsNullOrEmpty(remote))
            {
                return (remote, false);
            }

            if (string.IsNullOrEmpty(locale))
            {
                return (remote, false);
            }

            var newLocale = localizationMapping == LocalizationMapping.Repository ? $".{locale}" : ".localization";
            var repoName = remote.Split(new char[] { '/', '\\' }).Last();
            var match = s_repoNameWithLocale.Match(repoName);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var originLocale = match.Groups[1].Value;
                return (remote.Replace(originLocale, newLocale), true);
            }

            return ($"{remote}{newLocale}", true);
        }
    }
}
