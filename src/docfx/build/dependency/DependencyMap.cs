// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyMap : ReadOnlyDictionary<Document, HashSet<DependencyItem>>
    {
        public static readonly DependencyMap Empty = new DependencyMap(new Dictionary<Document, HashSet<DependencyItem>>());

        public DependencyMap(Dictionary<Document, HashSet<DependencyItem>> map)
            : base(map)
        {
        }

        public object ToDependencyMapModel()
        {
            // TODO: Make dependency map a data model once we remove legacy.
            var dependencies = this.ToDictionary(
                    d => d.Key.FilePath,
                    d => (from r in d.Value
                                  orderby r.To.FilePath descending, r.Type descending
                                  select r).Select(v => new DependencyManifestItem
                     {
                         Source = v.To.FilePath,
                         Type = v.Type,
                     }).ToArray());

            return new { dependencies };
        }
    }
}
