// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapItemComparer : IEqualityComparer<PublishUrlMapItem>
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
    }
}
