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
        private readonly Input _input;
        private readonly Config _config;

        private readonly (Func<string, bool>, FileMappingConfig)[] _globs;

        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private readonly HashSet<PathString> _fileNames;

        private readonly ConcurrentDictionary<PathString, (PathString, FileMappingConfig)> _fileMappings
                   = new ConcurrentDictionary<PathString, (PathString, FileMappingConfig)>();

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public HashSet<FilePath> Files { get; } = new HashSet<FilePath>();

        public BuildScope(Config config, Input input, Docset fallbackDocset)
        {
            _input = input;
            _config = config;

            _globs = CreateGlobs(config);

            var files = GetFiles(FileOrigin.Default);

            var fallbackFiles = fallbackDocset != null ? GetFiles(FileOrigin.Fallback) : Array.Empty<FilePath>();

            var fileNames = files.Select(file => file.Path).ToHashSet();

            Files.UnionWith(fallbackFiles.Where(file => !_fileNames.Contains(file.Path)));

            foreach (var (dependencyName, dependency) in _config.Dependencies)
            {
                if (dependency.IncludeInBuild)
                {
                    Files.UnionWith(GetFiles(FileOrigin.Dependency, dependencyName));
                }
            }

            _fileNames = Files.Select(file => file.Path).ToHashSet();
        }

        public bool Glob(PathString path)
        {
            var (mappedPath, _) = MapPath(path);
            return !mappedPath.IsEmpty;
        }

        public (PathString, FileMappingConfig) MapPath(PathString path)
        {
            return _fileMappings.GetOrAdd(path, _ =>
            {
                foreach (var (glob, mapping) in _globs)
                {
                    if (path.StartsWithPath(mapping.Src, out var remainingPath) && glob(remainingPath))
                    {
                        return (remainingPath + mapping.Dest, mapping);
                    }
                }
                return default;
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

        private IReadOnlyList<FilePath> GetFiles(FileOrigin origin, PathString? dependencyName = null)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<FilePath>();
                var fileNames = _input.ListFilesRecursive(origin, dependencyName);

                ParallelUtility.ForEach(fileNames, file =>
                {
                    if (Glob(file.Path))
                    {
                        files.Add(file);
                    }
                });

                return files.ToList();
            }
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
            return (
                from mapping in config.Content.Concat(config.Resource)
                let glob = GlobUtility.CreateGlobMatcher(
                    mapping.Files, mapping.Exclude.Concat(Config.DefaultExclude).ToArray())
                select (glob, mapping)).ToArray();
        }
    }
}
