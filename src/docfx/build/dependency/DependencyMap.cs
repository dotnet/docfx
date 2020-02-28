// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable

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
            var dependencies = this
                .OrderBy(d => d.Key.FilePath)
                .ToDictionary(
                    d => d.Key.FilePath.Path,
                    d => (from v in d.Value
                          orderby v.To.FilePath.Path, v.Type
                          select new DependencyManifestItem(v.To.FilePath.Path, v.Type)).ToArray());

            return new { dependencies };
        }
    }
}
