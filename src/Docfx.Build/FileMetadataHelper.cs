// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Glob;

namespace Docfx.Build.Engine;

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

        return changedGlobMatchers.Distinct();
    }

    private static IEnumerable<GlobMatcher> GetChangedGlobsByGroup(ImmutableArray<FileMetadataItem> leftGroupItems, ImmutableArray<FileMetadataItem> rightGroupItems)
    {
        var commonItems = leftGroupItems.GetLongestCommonSequence(rightGroupItems);
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
}
