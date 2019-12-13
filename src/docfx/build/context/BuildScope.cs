// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class BuildScope
    {
        private readonly Config _config;
        private readonly (Func<string, bool>, FileMappingConfig)[] _contentGlobs;
        private readonly (Func<string, bool>, FileMappingConfig)[] _resourceGlobs;

        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private readonly HashSet<PathString> _fileNames = new HashSet<PathString>();

        private readonly ConcurrentDictionary<PathString, (PathString, FileMappingConfig)> _fileMappings
                   = new ConcurrentDictionary<PathString, (PathString, FileMappingConfig)>();

        private readonly ConcurrentDictionary<PathString, (PathString, FileMappingConfig)> _resourceMappings
                   = new ConcurrentDictionary<PathString, (PathString, FileMappingConfig)>();

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public HashSet<FilePath> Files { get; }

        public BuildScope(Config config, Input input, Docset fallbackDocset)
        {
            _config = config;
            _contentGlobs = CreateGlobs(config, config.Content);
            _resourceGlobs = CreateGlobs(config, config.Resource);

            using (Progress.Start("Globbing files"))
            {
                var (fileNames, allFiles) = ListFiles(config, input, fallbackDocset);

                var files = new ListBuilder<FilePath>();
                ParallelUtility.ForEach(allFiles, file =>
                {
                    if (Glob(file.Path))
                    {
                        files.Add(file);
                    }
                });

                Files = files.ToList().ToHashSet();
                _fileNames = fileNames;
            }
        }

        public bool GlobResource(PathString path)
        {
            return MapResource(path, _resourceGlobs).mapping != null;
        }

        public bool Glob(PathString path)
        {
            return MapPath(path).mapping != null;
        }

        public (PathString path, FileMappingConfig mapping) MapPath(PathString path)
        {
            return MapPath(path, _contentGlobs.Concat(_resourceGlobs).ToArray());
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
            if ((filePath.ContentType == ContentType.Page || filePath.ContentType == ContentType.TableOfContents) && !Files.Contains(filePath.FilePath))
            {
                return true;
            }

            return false;
        }

        public bool GetActualFileName(PathString fileName, out PathString actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        private (PathString path, FileMappingConfig mapping) MapResource(PathString path, (Func<string, bool>, FileMappingConfig)[] globs)
        {
            return _resourceMappings.GetOrAdd(path, _ =>
            {
                foreach (var (glob, mapping) in globs)
                {
                    if (path.StartsWithPath(mapping.Src, out var remainingPath) && glob(remainingPath))
                    {
                        return (mapping.Dest + remainingPath, mapping);
                    }
                }
                return (path, null);
            });
        }

        private (PathString path, FileMappingConfig mapping) MapPath(PathString path, (Func<string, bool>, FileMappingConfig)[] globs)
        {
            return _fileMappings.GetOrAdd(path, _ =>
            {
                foreach (var (glob, mapping) in globs)
                {
                    if (path.StartsWithPath(mapping.Src, out var remainingPath) && glob(remainingPath))
                    {
                        return (mapping.Dest + remainingPath, mapping);
                    }
                }
                return (path, null);
            });
        }

        private static (HashSet<PathString> fileNames, HashSet<FilePath> files) ListFiles(Config config, Input input, Docset fallbackDocset)
        {
            var files = new HashSet<FilePath>();
            var fileNames = new HashSet<PathString>();

            var defaultFiles = input.ListFilesRecursive(FileOrigin.Default);
            files.UnionWith(defaultFiles);
            fileNames.UnionWith(defaultFiles.Select(file => file.Path));

            if (fallbackDocset != null)
            {
                var fallbackFiles = input.ListFilesRecursive(FileOrigin.Fallback)
                    .Where(file => !fileNames.Contains(file.Path));

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

        private static (Func<string, bool>, FileMappingConfig)[] CreateGlobs(Config config, FileMappingConfig[] mappings)
        {
            if (mappings.Length == 0)
            {
                var glob = GlobUtility.CreateGlobMatcher(
                    mappings.SelectMany(x => x.Files).ToArray(), config.Exclude.Concat(Config.DefaultExclude).ToArray());

                return new[] { (glob, new FileMappingConfig()) };
            }

            // Support v2 src/dest config per file group
            return (
                from mapping in mappings
                let glob = GlobUtility.CreateGlobMatcher(
                    mapping.Files, mapping.Exclude.Concat(Config.DefaultExclude).ToArray())
                select (glob, mapping)).ToArray();
        }
    }
}
