// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Docs.Build
{
    internal class SourceMap
    {
        private readonly IDictionary<PathString, PathString> _map = new Dictionary<PathString, PathString>();

        public SourceMap(ErrorBuilder errors, PathString docsetPath, Config config, FileResolver fileResolver)
        {
            foreach (var sourceMap in config.SourceMap)
            {
                if (!string.IsNullOrEmpty(sourceMap))
                {
                    var content = fileResolver.ReadString(sourceMap);
                    var map = JsonUtility.DeserializeData<SourceMapModel>(content, new FilePath(sourceMap));
                    var sourceMapDirectory = Path.GetDirectoryName(fileResolver.ResolveFilePath(sourceMap)) ?? "";

                    foreach (var (path, originalPath) in map.Files)
                    {
                        if (originalPath != null)
                        {
                            var key = new PathString(Path.GetRelativePath(docsetPath, Path.Combine(sourceMapDirectory, path)));
                            var value = new PathString(Path.GetRelativePath(docsetPath, Path.Combine(sourceMapDirectory, originalPath.Value)));
                            if (!_map.TryAdd(key, value))
                            {
                                errors.Add(Errors.SourceMap.DuplicateSourceMapItem(key, new List<PathString> { _map[key], value }));
                            }
                        }
                    }
                }
            }
        }

        public PathString? GetOriginalFilePath(FilePath path)
        {
            if (path.Origin == FileOrigin.Main && _map.TryGetValue(path.Path, out var originalPath))
            {
                return originalPath;
            }
            return null;
        }

        private class SourceMapModel
        {
            public Dictionary<PathString, PathString?> Files { get; } = new Dictionary<PathString, PathString?>();
        }
    }
}
