// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class SourceMap
    {
        private readonly IDictionary<PathString, PathString> _map = new Dictionary<PathString, PathString>();

        public SourceMap(PathString docsetPath, Config config, FileResolver fileResolver)
        {
            if (!string.IsNullOrEmpty(config.SourceMap))
            {
                var content = fileResolver.ReadString(config.SourceMap);
                var map = JsonUtility.Deserialize<Dictionary<PathString, PathString?>>(content, new FilePath(config.SourceMap));
                var sourceMapDirectory = Path.GetDirectoryName(fileResolver.ResolveFilePath(config.SourceMap)) ?? "";

                foreach (var (path, originalPath) in map)
                {
                    if (originalPath != null)
                    {
                        _map.Add(
                            new PathString(Path.GetRelativePath(docsetPath, Path.Combine(sourceMapDirectory, path))),
                            new PathString(Path.GetRelativePath(docsetPath, Path.Combine(sourceMapDirectory, originalPath.Value))));
                    }
                }
            }
        }

        public PathString? GetOriginalFilePath(PathString path)
        {
            return _map.TryGetValue(path, out var originalPath) ? (PathString?)originalPath : null;
        }
    }
}
