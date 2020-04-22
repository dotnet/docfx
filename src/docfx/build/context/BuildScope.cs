// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildScope
    {
        private readonly Config _config;
        private readonly (Func<string, bool>, FileMappingConfig)[] _globs;
        private readonly Input _input;
        private readonly Func<string, bool>[] _resourceGlobs;
        private readonly HashSet<string> _configReferences;
        private readonly BuildOptions _buildOptions;
        private readonly ErrorLog _errorLog;

        private readonly ConcurrentDictionary<FilePath, ContentType> _files = new ConcurrentDictionary<FilePath, ContentType>();

        private readonly ConcurrentDictionary<PathString, (PathString, FileMappingConfig?)> _fileMappings
                   = new ConcurrentDictionary<PathString, (PathString, FileMappingConfig?)>();

        private readonly ConcurrentDictionary<FilePath, SourceInfo<string?>> _mimeTypeCache
                   = new ConcurrentDictionary<FilePath, SourceInfo<string?>>();

        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private HashSet<PathString> _fileNames = new HashSet<PathString>();

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public IEnumerable<FilePath> Files => _files.Keys;

        public BuildScope(ErrorLog errorLog, Config config, Input input, BuildOptions buildOptions)
        {
            _config = config;
            _globs = CreateGlobs(config);
            _input = input;
            _buildOptions = buildOptions;
            _errorLog = errorLog;
            _resourceGlobs = CreateResourceGlob(config);
            _configReferences = config.Extend.Concat(config.GetFileReferences()).Select(path => path.Value).ToHashSet(PathUtility.PathComparer);

            using (Progress.Start("Globing files"))
            {
                var (fileNames, allFiles) = ListFiles(_config, _input, _buildOptions);

                ParallelUtility.ForEach(_errorLog, allFiles, file =>
                {
                    if (Glob(file.Path))
                    {
                        _files.TryAdd(file, GetContentType(file));
                    }
                });

                _fileNames = fileNames;
            }
        }

        public IEnumerable<FilePath> GetFiles(ContentType contentType)
        {
            return from pair in _files where pair.Value == contentType select pair.Key;
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

            if (name.Equals("TOC", PathUtility.PathComparison) || name.Equals("TOC.experimental", PathUtility.PathComparison))
            {
                return ContentType.TableOfContents;
            }

            return ContentType.Page;
        }

        public SourceInfo<string?> GetMime(ContentType contentType, FilePath filePath)
        {
            return _mimeTypeCache.GetOrAdd(filePath, path =>
            {
                return contentType == ContentType.Page ? ReadMimeFromFile(_input, path) : default;
            });
        }

        public bool Glob(PathString path)
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

        public bool OutOfScope(Document filePath)
        {
            // Link to dependent repo
            if (filePath.FilePath.Origin == FileOrigin.Dependency &&
                !_config.Dependencies[filePath.FilePath.DependencyName].IncludeInBuild)
            {
                return true;
            }

            // Pages outside build scope, don't build the file, leave href as is
            if ((filePath.ContentType == ContentType.Page || filePath.ContentType == ContentType.TableOfContents) && !_files.ContainsKey(filePath.FilePath))
            {
                return true;
            }

            return false;
        }

        public bool GetActualFileName(PathString fileName, out PathString actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        private static (HashSet<PathString> fileNames, HashSet<FilePath> files) ListFiles(Config config, Input input, BuildOptions buildOptions)
        {
            var files = new HashSet<FilePath>();
            var fileNames = new HashSet<PathString>();

            var defaultFiles = input.ListFilesRecursive(FileOrigin.Main);
            files.UnionWith(defaultFiles);
            fileNames.UnionWith(defaultFiles.Select(file => file.Path));

            if (buildOptions.IsLocalizedBuild)
            {
                var fallbackFiles = input.ListFilesRecursive(FileOrigin.Fallback).Where(file => !fileNames.Contains(file.Path));
                files.UnionWith(fallbackFiles);
                fileNames.UnionWith(fallbackFiles.Select(file => file.Path));
            }

            foreach (var (dependencyName, dependency) in config.Dependencies)
            {
                var depFiles = input.ListFilesRecursive(FileOrigin.Dependency, dependencyName);
                fileNames.UnionWith(depFiles.Select(file => file.Path));

                if (dependency.IncludeInBuild)
                {
                    files.UnionWith(depFiles);
                }
            }

            return (fileNames, files);
        }

        private static (Func<string, bool>, FileMappingConfig)[] CreateGlobs(Config config)
        {
            if (config.Content.Length == 0 && config.Resource.Length == 0)
            {
                var glob = GlobUtility.CreateGlobMatcher(
                    config.Files, config.Exclude.Concat(Config.DefaultExclude).ToArray());

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
                    select GlobUtility.CreateGlobMatcher(
                        mapping.Files, mapping.Exclude.Concat(Config.DefaultExclude).ToArray()))
                        .ToArray();
        }

        private static SourceInfo<string?> ReadMimeFromFile(Input input, FilePath filePath)
        {
            switch (filePath.Format)
            {
                // TODO: we could have not depend on this exists check, but currently
                //       LinkResolver works with Document and return a Document for token files,
                //       thus we are forced to get the mime type of a token file here even if it's not useful.
                //
                //       After token resolve does not create Document, this Exists check can be removed.
                case FileFormat.Json when input.Exists(filePath):
                    using (var reader = input.ReadText(filePath))
                    {
                        return JsonUtility.ReadMime(reader, filePath);
                    }
                case FileFormat.Yaml when input.Exists(filePath):
                    using (var reader = input.ReadText(filePath))
                    {
                        return new SourceInfo<string?>(YamlUtility.ReadMime(reader), new SourceInfo(filePath, 1, 1));
                    }
                default:
                    return default;
            }
        }
    }
}
