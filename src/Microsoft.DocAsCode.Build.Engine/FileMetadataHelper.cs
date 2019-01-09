// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Glob;

    using System.Collections.Generic;
    using System.Linq;

    public static class FileMetadataHelper
    {
        public static IEnumerable<GlobMatcher> GetChangedGlobs(this FileMetadata left, FileMetadata right)
        {
            var leftItems = left.SelectMany(l => l.Value);
            var rightItems = right.SelectMany(r => r.Value);
            var changedGlobMatchers = new List<GlobMatcher>();
            if (!string.Equals(left.BaseDir, right.BaseDir))
            {
                changedGlobMatchers.AddRange(leftItems.Select(l => l.Glob));
                changedGlobMatchers.AddRange(leftItems.Select(r => r.Glob));
            }
            else
            {
                foreach (var leftItem in leftItems)
                {
                    if (!rightItems.Any(rightItem => rightItem.Equals(leftItem)))
                    {
                        changedGlobMatchers.Add(leftItem.Glob);
                    }
                }
                foreach (var rightItem in rightItems)
                {
                    if (!leftItems.Any(leftItem => leftItem.Equals(rightItem)))
                    {
                        changedGlobMatchers.Add(rightItem.Glob);
                    }
                }
            }
            
            return changedGlobMatchers.Distinct();
        }
    }
}
