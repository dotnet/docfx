// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

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
        public static int LevenshteinDistance(this string src, string target)
        {
            // for all i and j, matrix[i,j] will hold the Levenshtein distance between
            // the first i characters of source and the first j characters of target
            // note that matrix has (src + 1) * (target + 1) values
            int srcLength = string.IsNullOrEmpty(src) ? 0 : src.Length;
            int targetLength = string.IsNullOrEmpty(target) ? 0 : target.Length;

            if (srcLength == 0)
            {
                return targetLength;
            }
            if (targetLength == 0)
            {
                return srcLength;
            }

            int[,] matrix = new int[srcLength + 1, targetLength + 1];

            // source prefixes can be transformed into empty string by
            // dropping all characters
            for (int i = 1; i <= srcLength; i++)
            {
                matrix[i, 0] = i;
            }

            // target prefixes can be reached from empty source prefix
            // by inserting every character
            for (int j = 1; j <= targetLength; j++)
            {
                matrix[0, j] = j;
            }

            for (int j = 1; j <= targetLength; j++)
            {
                for (int i = 1; i <= srcLength; i++)
                {
                    int cost = src[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[i, j] = new int[]
                    {
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1,
                        matrix[i - 1, j - 1] + cost,
                    }.Min();
                }
            }
            return matrix[srcLength, targetLength];
        }
    }
}
