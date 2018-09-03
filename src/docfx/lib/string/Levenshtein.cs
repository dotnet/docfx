// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Levenshtein
    {
        private static readonly ArrayPool<int> s_arrayPool = ArrayPool<int>.Shared;

        /// <summary>
        /// Calculate the Levenshtein Distance from src to target.
        /// </summary>
        /// <param name="src"> The source string </param>
        /// <param name="target">The target string </param>
        /// <returns>Levenshtein Distance</returns>
        public static int GetLevenshteinDistance(string src, string target)
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

            // ;new int[srcLength + 1, targetLength + 1];
            int[] matrix = s_arrayPool.Rent((srcLength + 1) * (targetLength + 1));

            matrix[0] = 0;
            // source prefixes can be transformed into empty string by
            // dropping all characters
            for (int i = 1; i <= srcLength; i++)
            {
                matrix[i] = i;
            }

            // target prefixes can be reached from empty source prefix
            // by inserting every character
            for (int j = 1; j <= targetLength; j++)
            {
                matrix[(j * (srcLength + 1)) + 1] = j;
            }

            for (int j = 1; j <= targetLength; j++)
            {
                for (int i = 1; i <= srcLength; i++)
                {
                    int cost = src[i - 1] == target[j - 1] ? 0 : 1;
                    matrix[(j * (srcLength + 1)) + i] = new int[]
                    {
                        matrix[(j * (srcLength + 1)) + i - 1] + 1,
                        matrix[((j - 1) * (srcLength + 1)) + i] + 1,
                        matrix[((j - 1) * (srcLength + 1)) + i - 1] + cost,
                    }.Min();
                }
            }
            int distance = matrix[((srcLength + 1) * (targetLength + 1)) - 1];
            s_arrayPool.Return(matrix);

            return distance;
        }
    }
}
