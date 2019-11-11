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
        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private readonly HashSet<PathString> _fileNames;
        private readonly Func<string, bool> _glob;

        private readonly Input _input;
        private readonly Docset _docset;
        private readonly DocumentProvider _documentProvider;

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public HashSet<Document> Files { get; }

        public Func<string, bool> Glob { get; }

        public BuildScope(Input input, DocumentProvider documentProvider, Docset docset, Docset fallbackDocset)
        {
            _input = input;
            _docset = docset;
            _documentProvider = documentProvider;

            Glob = CreateGlob(_docset.Config);

            var (fileNames, files) = GetFiles(FileOrigin.Default, Glob);

            var fallbackFiles = fallbackDocset != null
                ? GetFiles(FileOrigin.Fallback, CreateGlob(fallbackDocset.Config)).files
                : Enumerable.Empty<Document>();

            _fileNames = fileNames;

            Files = files.Concat(fallbackFiles.Where(file => !_fileNames.Contains(file.FilePath.Path))).ToHashSet();

            foreach (var (dependencyName, dependency) in _docset.Config.Dependencies)
            {
                if (dependency.IncludeInBuild)
                {
                    var (_, dependencyFiles) = GetFiles(FileOrigin.Dependency, Glob, dependencyName);
                    Files.UnionWith(dependencyFiles);
                }

                _fileNames.UnionWith(_input.ListFilesRecursive(FileOrigin.Dependency, dependencyName).Select(f => f.Path).ToList());
            }
        }

        public bool OutOfScope(Document filePath)
        {
            // Link to dependent repo
            if (filePath.FilePath.Origin == FileOrigin.Dependency &&
                !_docset.Config.Dependencies[filePath.FilePath.DependencyName].IncludeInBuild)
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

        private (HashSet<PathString> fileNames, IReadOnlyList<Document> files) GetFiles(
            FileOrigin origin, Func<string, bool> glob, PathString? dependencyName = null)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();
                var fileNames = _input.ListFilesRecursive(origin, dependencyName);

                ParallelUtility.ForEach(fileNames, file =>
                {
                    if (glob(file.Path.Value))
                    {
                        files.Add(_documentProvider.GetDocument(file));
                    }
                });

                return (fileNames.Select(item => item.Path).ToHashSet(), files.ToList());
            }
        }

        private static Func<string, bool> CreateGlob(Config config)
        {
            if (config.FileGroups.Length > 0)
            {
                var globs = config.FileGroups.Select(fileGroup => GlobUtility.CreateGlobMatcher(
                    fileGroup.Files,
                    fileGroup.Exclude.Concat(Config.DefaultExclude).ToArray()))
                   .ToArray();
                return new Func<string, bool>((file) => globs.Any(glob => glob(file)));
            }

            return GlobUtility.CreateGlobMatcher(
                config.Files,
                config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }
    }
}
