// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyMapBuilder
    {
        private readonly HashSet<DependencyItem> _dependencyItems = new HashSet<DependencyItem>();
        private readonly Dictionary<Document, HashSet<DependencyItem>> _inclusionDependencyItems = new Dictionary<Document, HashSet<DependencyItem>>();

        public void AddDependencyItem(Document root, Document relativeTo, Document dependencyDoc, DependencyType type)
        {
            Debug.Assert(root != null);
            Debug.Assert(relativeTo != null);

            if (dependencyDoc == null)
            {
                return;
            }

            if (relativeTo.Equals(root))
            {
                _dependencyItems.Add(new DependencyItem(dependencyDoc, type));
            }
            else
            {
                if (!_inclusionDependencyItems.TryGetValue(relativeTo, out var inclusionDependencyItems))
                {
                    inclusionDependencyItems = _inclusionDependencyItems[relativeTo] = new HashSet<DependencyItem>();
                }

                inclusionDependencyItems.Add(new DependencyItem(dependencyDoc, type));
            }
        }

        public DependencyMap Build()
        {
            return new DependencyMap(_dependencyItems, _inclusionDependencyItems.ToDictionary(k => k.Key, v => (IEnumerable<DependencyItem>)v.Value));
        }
    }
}
