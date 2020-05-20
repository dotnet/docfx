// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishItemComparer : IEqualityComparer<PublishItem>
    {
        public bool Equals(PublishItem? x, PublishItem? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return OutputPathEquals(x, y) || PublishUrlEquals(x, y);
        }

        public int GetHashCode(PublishItem obj)
        {
            return PathUtility.PathComparer.GetHashCode(obj.Url);
        }

        public static bool PublishUrlEquals(PublishItem x, PublishItem y)
        {
            return PathUtility.PathComparer.Compare(x.Url, y.Url) == 0
                  && (x.Monikers.Length == 0
                  || y.Monikers.Length == 0
                  || x.Monikers.Intersect(y.Monikers).Any());
        }

        public static bool OutputPathEquals(PublishItem x, PublishItem y)
        {
            return x.Path != null && y.Path != null && PathUtility.PathComparer.Compare(x.Path, y.Path) == 0;
        }
    }
}
