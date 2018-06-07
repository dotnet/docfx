// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Docs.Build
{
    internal class DependencyMap
    {
        public IEnumerable<DependencyItem> Dependencies { get; private set; }

        public Dictionary<Document, IEnumerable<DependencyItem>> InclusionDependencies { get; private set; }

        public DependencyMap(IEnumerable<DependencyItem> dependencies, Dictionary<Document, IEnumerable<DependencyItem>> inclusionDependencies = null)
        {
            Debug.Assert(dependencies != null);

            Dependencies = dependencies;
            InclusionDependencies = inclusionDependencies;
        }
    }
}
