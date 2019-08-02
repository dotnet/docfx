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
        private readonly TemplateEngine _templateEngine;

        /// <summary>
        /// Gets all the files to build, including redirections and fallback files.
        /// </summary>
        public HashSet<Document> Files { get; }

        public RedirectionMap Redirections { get; }

        public Docset Docset { get; }

        public Docset FallbackDocset { get; }

        public BuildScope(ErrorLog errorLog, Docset docset, Docset fallbackDocset, TemplateEngine templateEngine)
        {
            var config = docset.Config;

            Docset = docset;
            FallbackDocset = fallbackDocset;

            _glob = CreateGlob(config);
            _templateEngine = templateEngine;

            var (fileNames, files) = GetFiles(docset, _glob);

            var fallbackFiles = fallbackDocset != null
                ? GetFiles(fallbackDocset, CreateGlob(fallbackDocset.Config)).files
                : Enumerable.Empty<Document>();

            _fileNames = fileNames;

            Redirections = RedirectionMap.Create(errorLog, docset, _glob, templateEngine);

            Files = files.Concat(fallbackFiles.Where(file => !_fileNames.Contains(file.FilePath.Path)))
                         .Concat(Redirections.Files)
                         .ToHashSet();
        }

        public bool GetActualFileName(string fileName, out string actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        public Docset GetFallbackDocset(Docset docset)
            => docset == Docset || IsFallbackDocset(docset) ? FallbackDocset : null;

        public bool IsFallbackDocset(Docset docset)
            => docset == FallbackDocset;

        private (HashSet<string> fileNames, IReadOnlyList<Document> files) GetFiles(Docset docset, Func<string, bool> glob)
        {
            using (Progress.Start("Globbing files"))
            {
                var docsetPath = docset.DocsetPath;
                var files = new ListBuilder<Document>();
                var fileNames = Directory
                    .GetFiles(docsetPath, "*.*", SearchOption.AllDirectories)
                    .Select(path => Path.GetRelativePath(docsetPath, path).Replace('\\', '/'))
                    .ToHashSet(PathUtility.PathComparer);

                ParallelUtility.ForEach(fileNames, file =>
                {
                    if (glob(file))
                    {
                        files.Add(Document.Create(docset, file, _templateEngine, isFallback: docset == FallbackDocset));
                    }
                });

                return (fileNames, files.ToList());
            }
        }

        private static Func<string, bool> CreateGlob(Config config)
        {
            return GlobUtility.CreateGlobMatcher(
                config.Files,
                config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }
    }
}
