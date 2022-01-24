// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Microsoft.Docs.Build;

internal class DependencyMap : ReadOnlyDictionary<FilePath, HashSet<DependencyItem>>
{
    public static readonly DependencyMap Empty = new(new Dictionary<FilePath, HashSet<DependencyItem>>());

    public DependencyMap(Dictionary<FilePath, HashSet<DependencyItem>> map)
        : base(map)
    {
    }

    public object ToDependencyMapModel()
    {
        // TODO: Make dependency map a data model once we remove legacy.
        var dependencies = this
            .OrderBy(d => d.Key)
            .ToDictionary(
                d => d.Key.Path,
                d => (from v in d.Value
                      orderby v.To.Path, v.Type
                      select new DependencyManifestItem(v.To.Path, v.Type)).ToArray());

        return new { dependencies };
    }
}
