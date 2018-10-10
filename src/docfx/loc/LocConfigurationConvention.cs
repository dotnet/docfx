// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocConfigConvention
    {
        private static readonly Regex s_repoNameWithLocale = new Regex(@"^.+?(\.[a-z]{2,4}-[a-z]{2,4}(-[a-z]{2,4})?)?$", RegexOptions.IgnoreCase);

        public static string GetEditRepository(this Config config, string locale)
            => GetLocRepository(config.LocMappingType, config.Contribution.Repository, locale, config.DefaultLocale);

        /// <summary>
        /// The loc repo name follows below conventions:
        /// source name                         -->     loc name
        /// {org}/{repo-name}                   -->     {org}/{repo-name}.{locale}
        /// {org}/{repo-name}.{source-locale}   -->     {org}/{repo-name}.{loc-locale}
        /// </summary>
        /// <param name="locale">The current build locale</param>
        /// <returns>The repo name with locale</returns>
        public static string GetLocRepository(LocMappingType locMappingType, string repository, string locale, string defaultLocale)
        {
            if (locMappingType != LocMappingType.Repository && locMappingType != LocMappingType.RepositoryAndFolder)
            {
                return repository;
            }

            if (string.Equals(locale, defaultLocale, System.StringComparison.OrdinalIgnoreCase))
            {
                return repository;
            }

            if (string.IsNullOrEmpty(repository))
            {
                return repository;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return repository;
            }

            var newLocale = locMappingType == LocMappingType.Repository ? $".{locale}" : ".localization";
            var repoName = repository.Split(new[] { '/', '\\' }).Last();
            var match = s_repoNameWithLocale.Match(repoName);
            if (match.Success && match.Groups.Count >= 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var originLocale = match.Groups[1].Value;
                return repository.Replace(originLocale, newLocale);
            }

            return $"{repository}{newLocale}";
        }
    }
}
