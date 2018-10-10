// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocConfigConvention
    {
        private static readonly Regex s_repoNameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?)?$", RegexOptions.IgnoreCase);

        public static (string owner, string repoName) GetEditRepository(this Config config, string locale)
        {
            var nameParts = config.Contribution.Repository?.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var repoName = nameParts?.Last();
            var orgName = nameParts?.First();
            return GetLocRepository(config.LocMappingType, orgName, repoName, locale, config.DefaultLocale);
        }

        /// <summary>
        /// The loc repo name follows below conventions:
        /// source name                         -->     loc name
        /// {org}/{repo-name}                   -->     {org}/{repo-name}.{locale}
        /// {org}/{repo-name}.{source-locale}   -->     {org}/{repo-name}.{loc-locale}
        /// // TODO: org name can be different
        /// </summary>
        /// <param name="locale">The current build locale</param>
        /// <returns>The repo name with locale</returns>
        public static (string owner, string repoName) GetLocRepository(LocMappingType locMappingType, string owner, string repoName, string locale, string defaultLocale)
        {
            if (locMappingType != LocMappingType.Repository && locMappingType != LocMappingType.RepositoryAndFolder)
            {
                return (owner, repoName);
            }

            if (string.Equals(locale, defaultLocale, System.StringComparison.OrdinalIgnoreCase))
            {
                return (owner, repoName);
            }

            if (string.IsNullOrEmpty(repoName))
            {
                return (owner, repoName);
            }

            if (string.IsNullOrEmpty(locale))
            {
                return (owner, repoName);
            }

            var newLocale = locMappingType == LocMappingType.Repository ? $".{locale}" : ".localization";
            var match = s_repoNameWithLocale.Match(repoName);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var originLocale = match.Groups[1].Value;
                return (owner, repoName.Replace(originLocale, newLocale));
            }

            return (owner, $"{repoName}{newLocale}");
        }
    }
}
