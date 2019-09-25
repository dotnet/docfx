// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            var dependencies = this
                .OrderBy(d => d.Key.FilePath.Path)
                .ThenBy(d => d.Key.FilePath.Origin)
                .ThenBy(d => d.Key.FilePath.DependencyName ?? "")
                .ToDictionary(
                    d => GetSourcePath(d.Key.FilePath),
                    d => (from v in d.Value
                          orderby v.To.FilePath.Path, v.To.FilePath.Origin, v.To.FilePath.DependencyName ?? "", v.Type
                          select new DependencyManifestItem { Source = GetSourcePath(v.To.FilePath), Type = v.Type }).ToArray());

            return new { dependencies };

            string GetSourcePath(FilePath file)
                => Path.Combine(file.DependencyName ?? "", file.Path).Replace("\\", "/");
        }
    }
}
