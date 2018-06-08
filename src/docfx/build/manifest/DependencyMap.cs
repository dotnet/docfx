// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class DependencyMap
    {
        public static readonly DependencyMap Empty = new DependencyMap(new ReadOnlyDictionary<Document, IEnumerable<DependencyItem>>(new Dictionary<Document, IEnumerable<DependencyItem>>()));

        public DependencyMap(IReadOnlyDictionary<Document, IEnumerable<DependencyItem>> map)
        {
            Debug.Assert(map != null);
            Map = map;
        }

        public IReadOnlyDictionary<Document, IEnumerable<DependencyItem>> Map { get; private set; }
    }
}
