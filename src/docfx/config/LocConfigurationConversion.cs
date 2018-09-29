// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class LocConfigConversion
    {
        private static readonly Regex s_repoNameWithLocale = new Regex(@"^.+?\.?([a-z]{2,4}-[a-z]{2,4})?$", RegexOptions.IgnoreCase);

        /// <summary>
        /// The eidt repo name for loc follows below conversions:
        /// source name                         -->     loc name
        /// {org}/{repo-name}                   -->     {org}/{repo-name}.{locale}
        /// {org}/{repo-name}.{source-locale}   -->     {org}/{repo-name}.{loc-locale}
        /// </summary>
        /// <param name="repository">The origin repo name</param>
        /// <param name="locale">The current build locale</param>
        /// <returns>The repo name with locale</returns>
        public static string GetEditRepository(string repository, string locale)
        {
            if (string.IsNullOrEmpty(repository))
            {
                return repository;
            }

            if (string.IsNullOrEmpty(locale))
            {
                return repository;
            }

            var match = s_repoNameWithLocale.Match(repository);
            if (match.Success && match.Groups.Count == 2 && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var originLocale = match.Groups[1].Value;
                return repository.Replace(originLocale, locale);
            }

            return $"{repository}.{locale}";
        }
    }
}
