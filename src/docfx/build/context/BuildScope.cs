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
        private readonly HashSet<PathString> _fileNames = new HashSet<PathString>();

        private readonly Config _config;

        /// <summary>
        /// Gets all the files and fallback files to build, excluding redirections.
        /// </summary>
        public HashSet<FilePath> Files { get; }

        public Func<string, bool> Glob { get; }

        public BuildScope(Config config, Input input, Docset fallbackDocset)
        {
            _config = config;

            using (Progress.Start("Globbing files"))
            {
                Glob = CreateGlob(config);

                var (fileNames, allFiles) = ListFiles(config, input, fallbackDocset);

                var files = new ListBuilder<FilePath>();
                ParallelUtility.ForEach(allFiles, file =>
                {
                    if (Glob(file.Path.Value))
                    {
                        files.Add(file);
                    }
                });

                Files = files.ToList().ToHashSet();
                _fileNames = fileNames;
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
