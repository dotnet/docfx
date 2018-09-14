// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class Levenshtein
    {
        /// <summary>
        /// Calculate the Levenshtein Distance from source string to target string.
        /// </summary>
        /// <param name="src"> The source string </param>
        /// <param name="target">The target string </param>
        /// <returns>Levenshtein Distance</returns>
        public static int GetLevenshteinDistance(string src, string target)
        {
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
            var srcCharArray = src.ToCharArray();
            var targetCharArray = target.ToCharArray();
            return GetLevenshteinDistance<char>(srcCharArray, targetCharArray, Comparer<char>.Default);
        }

        /// <summary>
        /// Get levenshtein distance of two generic type list.
        /// </summary>
        /// <typeparam name="T"> Generic type, e.g. string</typeparam>
        /// <param name="src">The source type list</param>
        /// <param name="target">The target type list</param>
        /// <param name="comparer">Implementation of IEqualityComparer for given type</param>
        /// <returns>Levenshtein distance of the two given list</returns>
        public static int GetLevenshteinDistance<T>(IList<T> src, IList<T> target, IComparer<T> comparer)
        {
            // for all i and j, matrix[i,j] will hold the Levenshtein distance between
            // the first i characters of source and the first j characters of target
            // note that matrix has (src + 1) * (target + 1) values
            int srcLength = src == null ? 0 : src.Count;
            int targetLength = target == null ? 0 : target.Count;

            if (srcLength == 0)
            {
                return targetLength;
            }
            if (targetLength == 0)
            {
                return srcLength;
            }

            int[] matrix = ArrayPool<int>.Shared.Rent((srcLength + 1) * (targetLength + 1));
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
                matrix[j * (srcLength + 1)] = j;
            }
            for (int j = 1; j <= targetLength; j++)
            {
                for (int i = 1; i <= srcLength; i++)
                {
                    int cost = comparer.Compare(src[i - 1], target[j - 1]) == 0 ? 0 : 1;
                    matrix[(j * (srcLength + 1)) + i] = Math.Min(
                        matrix[(j * (srcLength + 1)) + i - 1] + 1, Math.Min(
                        matrix[((j - 1) * (srcLength + 1)) + i] + 1,
                        matrix[((j - 1) * (srcLength + 1)) + i - 1] + cost));
                }
            }
            int distance = matrix[((srcLength + 1) * (targetLength + 1)) - 1];

            ArrayPool<int>.Shared.Return(matrix);

            return distance;
        }
    }
}
