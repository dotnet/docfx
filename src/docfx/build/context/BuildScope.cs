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
        private readonly Input _input;
        private readonly Config _config;
        private readonly DocumentProvider _documentProvider;

        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private readonly HashSet<PathString> _fileNames;
        private readonly FileMappingConfig[] _groups;

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public HashSet<Document> Files { get; }

        public Func<string, bool> Glob { get; }

        public BuildScope(Config config, Input input, DocumentProvider documentProvider, Docset fallbackDocset)
        {
            _input = input;
            _config = config;
            _documentProvider = documentProvider;

            _groups = _config.Content.Concat(_config.Resource).ToArray();

            Glob = CreateGlob();

            var (fileNames, files) = GetFiles(FileOrigin.Default);

            var fallbackFiles = fallbackDocset != null ? GetFiles(FileOrigin.Fallback).files : Enumerable.Empty<Document>();

            _fileNames = fileNames;

            Files = files.Concat(fallbackFiles.Where(file => !_fileNames.Contains(file.FilePath.Path))).ToHashSet();

            foreach (var (dependencyName, dependency) in _config.Dependencies)
            {
                if (dependency.IncludeInBuild)
                {
                    var (_, dependencyFiles) = GetFiles(FileOrigin.Dependency, dependencyName);
                    Files.UnionWith(dependencyFiles);
                }

                _fileNames.UnionWith(_input.ListFilesRecursive(FileOrigin.Dependency, dependencyName).Select(f => f.Path).ToList());
            }
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
            if ((filePath.ContentType == ContentType.Page || filePath.ContentType == ContentType.TableOfContents) && !Files.Contains(filePath))
            {
                return true;
            }

            return false;
        }

        public bool GetActualFileName(PathString fileName, out PathString actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        public bool TryGetFileMapping(FilePath path, out FileMappingConfig result)
        {
            foreach (var group in _groups)
            {
                if (path.Path.Value.StartsWith(group.Src))
                {
                    result = group;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private (HashSet<PathString> fileNames, IReadOnlyList<Document> files) GetFiles(
            FileOrigin origin, PathString? dependencyName = null)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();
                var fileNames = _input.ListFilesRecursive(origin, dependencyName);

                ParallelUtility.ForEach(fileNames, file =>
                {
                    if (Glob(file.Path.Value))
                    {
                        files.Add(_documentProvider.GetDocument(file));
                    }
                });

                return (fileNames.Select(item => item.Path).ToHashSet(), files.ToList());
            }
        }

        private Func<string, bool> CreateGlob()
        {
            if (_groups.Length > 0)
            {
                var globs = _groups.Select(group => GlobUtility.CreateGlobMatcher(
                    group.Files.Select(item => Path.Combine(group.Src, item)).ToArray(),
                    group.Exclude.Concat(Config.DefaultExclude).Select(item => Path.Combine(group.Src, item)).ToArray()))
                   .ToArray();

                return new Func<string, bool>((file) => globs.Any(glob => glob(file)));
            }

            return GlobUtility.CreateGlobMatcher(
                _config.Files,
                _config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }
    }
}
