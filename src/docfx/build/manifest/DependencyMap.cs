// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Docs.Build
{
    internal class DependencyMap : ReadOnlyDictionary<Document, IEnumerable<DependencyItem>>
    {
        public static readonly DependencyMap Empty = new DependencyMap(new Dictionary<Document, IEnumerable<DependencyItem>>());

        public DependencyMap(Dictionary<Document, IEnumerable<DependencyItem>> map)
            : base(map)
        {
        }
    }
}
