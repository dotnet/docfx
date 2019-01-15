// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DependencyMap : ReadOnlyDictionary<Document, List<DependencyItem>>
    {
        public static readonly DependencyMap Empty = new DependencyMap(new Dictionary<Document, List<DependencyItem>>());

        public DependencyMap(Dictionary<Document, List<DependencyItem>> map)
            : base(map)
        {
        }

        public object ToDependencyMapModel()
        {
            // TODO: Make dependency map a data model once we remove legacy.
            var dependencies = this.ToDictionary(
                    d => d.Key.FilePath,
                    d => d.Value.Select(v => new DependencyManifestItem
                    {
                        Source = v.Dest.FilePath,
                        Type = v.Type,
                    }).ToArray());

            return new { dependencies };
        }
    }
}
