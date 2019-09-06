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
        // On a case insensitive system, cannot simply get the actual file casing:
        // https://github.com/dotnet/corefx/issues/1086
        // This lookup table stores a list of actual filenames.
        private readonly HashSet<string> _fileNames;
        private readonly Func<string, bool> _glob;

        private readonly Input _input;
        private readonly TemplateEngine _templateEngine;

        /// <summary>
        /// Gets all the files to build, including redirections and fallback files.
        /// </summary>
        public HashSet<Document> Files { get; }

        public RedirectionMap Redirections { get; }

        public HashSet<string> InScopeDependencyNames { get; } = new HashSet<string>();

        public BuildScope(ErrorLog errorLog, Input input, Docset docset, Docset fallbackDocset, Dictionary<string, (Docset docset, bool inScope)> dependencyDocsets, TemplateEngine templateEngine)
        {
            var config = docset.Config;

            _input = input;
            _glob = CreateGlob(config);
            _templateEngine = templateEngine;

            var (fileNames, files) = GetFiles(FileOrigin.Default, docset, _glob);

            var fallbackFiles = fallbackDocset != null
                ? GetFiles(FileOrigin.Fallback, fallbackDocset, CreateGlob(fallbackDocset.Config)).files
                : Enumerable.Empty<Document>();

            _fileNames = fileNames;

            Files = files.Concat(fallbackFiles.Where(file => !_fileNames.Contains(file.FilePath.Path))).ToHashSet();

            Redirections = RedirectionMap.Create(errorLog, docset, _glob, _input, templateEngine, Files);

            Files.UnionWith(Redirections.Files);

            foreach (var (dependencyName, (dependencyDocset, inScope)) in dependencyDocsets)
            {
                if (inScope)
                {
                    InScopeDependencyNames.Add(dependencyName);
                    var (_, dependencyFiles) = GetFiles(FileOrigin.Dependency, dependencyDocset, _glob);
                    Files.UnionWith(dependencyFiles);
                }
            }
        }

        public bool GetActualFileName(string fileName, out string actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        private (HashSet<string> fileNames, IReadOnlyList<Document> files) GetFiles(
            FileOrigin origin, Docset docset, Func<string, bool> glob, string dependencyName = null)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();
                var fileNames = _input.ListFilesRecursive(origin, dependencyName);

                ParallelUtility.ForEach(fileNames, file =>
                {
                    if (glob(file.Path))
                    {
                        files.Add(Document.Create(docset, file, _input, _templateEngine));
                    }
                });

                return (fileNames.Select(item => item.Path).ToHashSet(PathUtility.PathComparer), files.ToList());
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
