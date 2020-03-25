// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal partial class PublishModelBuilder
    {
        public class PublishItemComparer : IEqualityComparer<PublishItem>
        {
            public bool Equals(PublishItem x, PublishItem y)
            {
                return PathUtility.PathComparer.Compare(x.Url, y.Url) == 0
                   && (x.Monikers.Length == 0
                   || y.Monikers.Length == 0
                   || x.Monikers.Intersect(y.Monikers).Any());
            }

            public int GetHashCode(PublishItem obj)
                => PathUtility.PathComparer.GetHashCode(obj.Url);
        }
    }
}
