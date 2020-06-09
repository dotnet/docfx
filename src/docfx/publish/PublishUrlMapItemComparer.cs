// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapItemComparer : IEqualityComparer<PublishUrlMapItem>, IComparer<PublishUrlMapItem>
    {
        public bool Equals(PublishUrlMapItem? x, PublishUrlMapItem? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return PublishUrlEquals(x, y);
        }

        public int GetHashCode(PublishUrlMapItem obj)
        {
            return PathUtility.PathComparer.GetHashCode(obj.Url);
        }

        public static bool PublishUrlEquals(PublishUrlMapItem x, PublishUrlMapItem y)
        {
            return PathUtility.PathComparer.Compare(x.Url, y.Url) == 0 && x.Monikers.Intersects(y.Monikers);
        }

        public int Compare([AllowNull] PublishUrlMapItem x, [AllowNull] PublishUrlMapItem y)
        {
            if (x is null)
            {
                if (y is null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }

            if (y is null)
            {
                return 1;
            }

            if (!x.Monikers.HasMonikers && y.Monikers.HasMonikers)
            {
                return 1;
            }
            else if (x.Monikers.HasMonikers && !y.Monikers.HasMonikers)
            {
                return -1;
            }

            var result = PathUtility.PathComparer.Compare(x.Monikers.MonikerGroup, y.Monikers.MonikerGroup);

            if (result == 0)
            {
                if (x.SourcePath.Origin == FileOrigin.Redirection && y.SourcePath.Origin == FileOrigin.Main)
                {
                    return 1;
                }
                else if (y.SourcePath.Origin == FileOrigin.Redirection && x.SourcePath.Origin == FileOrigin.Main)
                {
                    return -1;
                }
            }

            if (result == 0)
            {
                result = PathUtility.PathComparer.Compare(x.SourcePath.Path, y.SourcePath.Path);
            }
            return result;
        }
    }
}
