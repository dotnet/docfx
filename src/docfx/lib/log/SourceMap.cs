// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build;

internal class SourceMap
{
    private readonly Dictionary<PathString, PathString> _map = new();
    private readonly ErrorBuilder _errors;

    public SourceMap(ErrorBuilder errors, PathString docsetPath, Config config, FileResolver fileResolver)
    {
        _errors = errors;
        foreach (var sourceMap in config.SourceMap)
        {
            if (!string.IsNullOrEmpty(sourceMap))
            {
                var content = fileResolver.TryReadString(sourceMap);
                if (content is null)
                {
                    continue;
                }

                var map = JsonUtility.DeserializeData<SourceMapModel>(content!, new FilePath(sourceMap));
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

    public void AddOriginalPath(PathString targetPath, PathString sourcePath)
    {
        if (!_map.TryAdd(targetPath, sourcePath))
        {
            _errors.Add(Errors.SourceMap.DuplicateSourceMapItem(targetPath, new List<PathString> { _map[targetPath], sourcePath }));
        }
    }

    public FilePath? GetOriginalFilePath(FilePath path)
    {
        if (path.Origin == FileOrigin.Main && _map.TryGetValue(path.Path, out var originalPath))
        {
            return FilePath.Content(originalPath);
        }
        return null;
    }

    private class SourceMapModel
    {
        public Dictionary<PathString, PathString?> Files { get; } = new Dictionary<PathString, PathString?>();
    }
}
