// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class StringUtility
    {
        /// <summary>
        /// Calculate the Levenshtein Distance from src to target.
        /// </summary>
        /// <param name="src"> The source string </param>
        /// <param name="target">The target string </param>
        /// <returns>Levenshtein Distance</returns>
        public static int TestDistance(string src, string target)
        {
            // splict src and targets
            var srcNames = Regex.Split(src, "[^a-zA-Z0-9]+").Where(str => !string.IsNullOrWhiteSpace(str)).ToList();
            var targetNames = Regex.Split(target, "[^a-zA-Z0-9]+").Where(str => !string.IsNullOrWhiteSpace(str)).ToList();
            return Levenshtein.GetLevenshteinDistance<string>(srcNames, targetNames, StringComparer.OrdinalIgnoreCase);
        }
    }
}
