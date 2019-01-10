// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    using Microsoft.DocAsCode.Glob;

    public static class FileMetadataHelper
    {
        public static IEnumerable<GlobMatcher> GetChangedGlobs(this FileMetadata left, FileMetadata right)
        {
            if (left == null && right == null)
            {
                return new List<GlobMatcher>();
            }
            if (left == null || right == null)
            {
                return left == null
                    ? right.GetAllGlobs()
                    : left.GetAllGlobs();
            }

            var changedGlobMatchers = new List<GlobMatcher>();
            if (!string.Equals(left.BaseDir, right.BaseDir))
            {
                changedGlobMatchers.AddRange(left.GetAllGlobs());
                changedGlobMatchers.AddRange(right.GetAllGlobs());
            }
            else
            {
                var handledRightKeys = new Dictionary<string, bool>();
                foreach (var leftItem in left)
                {
                    if (right.TryGetValue(leftItem.Key, out var rightFileMetadataItemsGroup))
                    {
                        handledRightKeys[leftItem.Key] = true;
                        var leftFileMetadataItemsGroup = leftItem.Value;

                        var changes = GetChangedGlobsByGroup(leftFileMetadataItemsGroup, rightFileMetadataItemsGroup);
                        changedGlobMatchers.AddRange(changes);
                    }
                    else
                    {
                        changedGlobMatchers.AddRange(leftItem.Value.Select(v => v.Glob));
                    }
                }
                foreach (var rightItem in right)
                {
                    if (!handledRightKeys.TryGetValue(rightItem.Key, out var _))
                    {
                        changedGlobMatchers.AddRange(rightItem.Value.Select(v => v.Glob));
                    }
                }
            }

            return changedGlobMatchers.Distinct();
        }

        private static IEnumerable<GlobMatcher> GetChangedGlobsByGroup(ImmutableArray<FileMetadataItem> leftGroupItems, ImmutableArray<FileMetadataItem> rightGroupItems)
        {
            var commonItems = GetLongestCommonSequence(leftGroupItems, rightGroupItems);
            var changes = new List<GlobMatcher>();
            foreach (var leftItem in leftGroupItems)
            {
                if (!commonItems.Any(i => i.Equals(leftItem)))
                {
                    changes.Add(leftItem.Glob);
                }
            }

            foreach (var rightItem in rightGroupItems)
            {
                if (!commonItems.Any(i => i.Equals(rightItem)))
                {
                    changes.Add(rightItem.Glob);
                }
            }

            return changes;
        }

        public static IList<T> GetLongestCommonSequence<T>(ImmutableArray<T> leftItems, ImmutableArray<T> rightItems)
        {
            int leftItemCount = leftItems.Count();
            int rightItemCount = rightItems.Count();
            int[,] dp = new int[leftItemCount + 1, rightItemCount + 1];

            for (int i = 0; i <= leftItemCount; i++)
            {
                for (int j = 0; j <= rightItemCount; j++)
                {
                    if (i == 0 || j == 0)
                    {
                        dp[i, j] = 0;
                    }
                    else if (leftItems[i - 1].Equals(rightItems[j - 1]))
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }
            int n = leftItemCount;
            int m = rightItemCount;
            var results = new List<T>();
            while (dp[n, m] > 0)
            {
                if (dp[n, m] == dp[n - 1, m])
                {
                    n--;
                }
                else if (dp[n, m] == dp[n, m - 1])
                {
                    m--;
                }
                else
                {
                    results.Add(leftItems[n - 1]);
                    n--;
                    m--;
                }
            }
            results.Reverse();
            return results;
        }
    }
}
