// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Docs.Build;

internal class BuildScope
{
    private readonly Config _config;
    private readonly BuildOptions _buildOptions;
    private readonly (Func<string, bool>, FileMappingConfig)[] _globs;
    private readonly Input _input;
    private readonly Func<string, bool>[] _resourceGlobs;
    private readonly HashSet<string> _configReferences;

    // On a case insensitive system, cannot simply get the actual file casing:
    // https://github.com/dotnet/corefx/issues/1086
    // This lookup table stores a list of actual filenames.
    private readonly Watch<(HashSet<FilePath> allFiles, IReadOnlyDictionary<FilePath, ContentType> files)> _files;

    private readonly ConcurrentDictionary<PathString, (PathString, FileMappingConfig?)> _fileMappings = new();

    /// <summary>
    /// Gets all the files and fallback files to build, excluding redirections.
    /// </summary>
    public IEnumerable<FilePath> Files => _files.Value.files.Keys;

    public BuildScope(Config config, Input input, BuildOptions buildOptions)
    {
        _config = config;
        _buildOptions = buildOptions;
        _globs = CreateGlobs(config);
        _input = input;
        _resourceGlobs = CreateResourceGlob(config);
        _configReferences = config.Extend.Concat(config.GetFileReferences()).Select(path => PathUtility.Normalize(path.Value))
            .ToHashSet(PathUtility.PathComparer);

        _files = new(GlobFiles);
    }

    public IEnumerable<FilePath> GetFiles(ContentType contentType)
    {
        return from pair in _files.Value.files where pair.Value == contentType select pair.Key;
    }

    public bool TryGetActualFilePath(FilePath path, [NotNullWhen(true)] out FilePath? actualPath)
    {
        if (_files.Value.allFiles.TryGetValue(path, out actualPath))
        {
            return true;
        }

        if (_input.Exists(path))
        {
            actualPath = path;
            return true;
        }

        return false;
    }

    public ContentType GetContentType(FilePath path)
    {
        return path.Origin == FileOrigin.Redirection ? ContentType.Redirection : GetContentType(path.Path);
    }

    public ContentType GetContentType(string path)
    {
        if (_configReferences.Contains(path))
        {
            return ContentType.Unknown;
        }

        var name = Path.GetFileNameWithoutExtension(path);
        if (name.Equals("docfx", PathUtility.PathComparison))
        {
            return ContentType.Unknown;
        }
        if (name.Equals("redirections", PathUtility.PathComparison))
        {
            return ContentType.Unknown;
        }

        foreach (var glob in _resourceGlobs)
        {
            if (glob(path))
            {
                return ContentType.Resource;
            }
        }

        if (!path.EndsWith(".md", PathUtility.PathComparison) &&
            !path.EndsWith(".json", PathUtility.PathComparison) &&
            !path.EndsWith(".yml", PathUtility.PathComparison))
        {
            return ContentType.Resource;
        }

        if (name.Equals("TOC", PathUtility.PathComparison))
        {
            return ContentType.Toc;
        }

        return ContentType.Page;
    }

    public bool Contains(PathString path)
    {
        return MapPath(path).mapping != null;
    }

    public (PathString path, FileMappingConfig? mapping) MapPath(PathString path)
    {
        return _fileMappings.GetOrAdd(path, _ =>
        {
            foreach (var (glob, mapping) in _globs)
            {
                if (path.StartsWithPath(mapping.Src, out var remainingPath) && glob(remainingPath))
                {
                    return (mapping.Dest.Concat(remainingPath), mapping);
                }
            }
            return (path, null);
        });
    }

    public bool OutOfScope(FilePath file)
    {
        return file.Origin switch
        {
            // Link to dependent repo
            FileOrigin.Dependency when !_config.Dependencies[file.DependencyName].IncludeInBuild => true,

            // Pages outside build scope, don't build the file, leave href as is
            FileOrigin.Main => !_files.Value.files.ContainsKey(file),
            _ => false,
        };
    }

    private (HashSet<FilePath>, IReadOnlyDictionary<FilePath, ContentType>) GlobFiles()
    {
        using (Progress.Start("Globing files"))
        {
            var allFiles = new HashSet<FilePath>();
            var files = new DictionaryBuilder<FilePath, ContentType>();

            var defaultFiles = _input.ListFilesRecursive(FileOrigin.Main);
            allFiles.UnionWith(defaultFiles);

            if (_buildOptions.IsLocalizedBuild)
            {
                var fileNames = defaultFiles.Select(file => file.Path).ToHashSet();
                var fallbackFiles = _input.ListFilesRecursive(FileOrigin.Fallback).Where(file => !fileNames.Contains(file.Path));
                allFiles.UnionWith(fallbackFiles);
            }

            Parallel.ForEach(allFiles, file =>
            {
                if (Contains(file.Path))
                {
                    files.TryAdd(file, GetContentType(file));
                }
            });

            Parallel.ForEach(_config.Dependencies, dep =>
            {
                var depFiles = _input.ListFilesRecursive(FileOrigin.Dependency, dep.Key);
                lock (allFiles)
                {
                    allFiles.AddRange(depFiles);
                }

                if (dep.Value.IncludeInBuild)
                {
                    Parallel.ForEach(depFiles, file =>
                    {
                        if (Contains(file.Path))
                        {
                            files.TryAdd(file, GetContentType(file));
                        }
                    });
                }
            });

            return (allFiles, files.AsDictionary());
        }
    }

    private static (Func<string, bool>, FileMappingConfig)[] CreateGlobs(Config config)
    {
        if (config.Content.Length == 0 && config.Resource.Length == 0)
        {
            var glob = GlobUtility.CreateGlobMatcher(config.Files, config.Exclude.Concat(Config.DefaultExclude).ToArray());
            return new[] { (glob, new FileMappingConfig()) };
        }

        // Support v2 src/dest config per file group
        return (from mapping in config.Content.Concat(config.Resource)
                let glob = GlobUtility.CreateGlobMatcher(
                    mapping.Files, mapping.Exclude.Concat(Config.DefaultExclude).ToArray())
                select (glob, mapping)).ToArray();
    }

    private static Func<string, bool>[] CreateResourceGlob(Config config)
    {
        return (from mapping in config.Resource
                select GlobUtility.CreateGlobMatcher(mapping.Files, mapping.Exclude.Concat(Config.DefaultExclude).ToArray())).ToArray();
    }
}
