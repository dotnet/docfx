// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Docs.Build
{
    internal class ResourceDependencyMap : ReadOnlyDictionary<Document, List<Document>>
    {
        public static readonly ResourceDependencyMap Empty = new ResourceDependencyMap(new Dictionary<Document, List<Document>>());

        public ResourceDependencyMap(Dictionary<Document, List<Document>> map)
            : base(map)
        {
        }
    }
}
