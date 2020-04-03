// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class StringUtility
    {
        public static string ToCamelCase(char wordSeparator, string value)
        {
            var sb = new StringBuilder();
            var words = value.ToLowerInvariant().Split(wordSeparator);
            sb.Length = 0;
            sb.Append(words[0]);
            for (var i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(words[i][0]));
                    sb.Append(words[i], 1, words[i].Length - 1);
                }
            }
            return sb.ToString().Trim();
        }

        public static string UpperCaseFirstChar(string value)
        {
            return value.Length == 0
                        ? value
                        : value.First().ToString().ToUpperInvariant() + value.Substring(1).ToLowerInvariant();
        }

        public static string Join<T>(IEnumerable<T> source, int take = 5)
        {
            var formatSource = source.Select(item => $"'{item}'").OrderBy(_ => _, StringComparer.Ordinal);
            return $"{string.Join(", ", formatSource.Take(take))}{(formatSource.Count() > 5 ? "..." : "")}";
        }

        /// <summary>
        /// Find the string that best matches <paramref name="target"/> from <paramref name="candidates"/>,
        /// return if a match is found and assigned the found value to  <paramref name="bestMatch"/> accordingly. <para/>
        /// Returns: false if no match is found, otherwise return true.
        /// </summary>
        /// <param name="target">The string to be looked for</param>
        /// <param name="candidates">Possible strings to look for from</param>
        /// <param name="bestMatch">If a match is found, this will be assigned</param>
        /// <param name="threshold">Max levenshtein distance between the candidate and the target, greater values will be filtered</param>
        /// <returns>
        ///     if a match is found, return true and assign it to <paramref name="bestMatch"/>, otherwise return false.
        /// </returns>
        public static bool FindBestMatch(string target, IEnumerable<string> candidates, [NotNullWhen(true)] out string? bestMatch, int threshold = 5)
        {
            bestMatch = candidates != null ?
                    (from candidate in candidates
                     let levenshteinDistance = Levenshtein.GetLevenshteinDistance(candidate, target)
                     where levenshteinDistance <= threshold
                     orderby levenshteinDistance, candidate
                     select candidate).FirstOrDefault()
                    : null;

            return !string.IsNullOrEmpty(bestMatch);
        }
    }
}
