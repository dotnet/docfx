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

        public HashSet<Document> Files { get; }

        public HashSet<Document> FilesWithFallback { get; }

        public RedirectionMap Redirections { get; }

        public BuildScope(ErrorLog errorLog, Docset docset)
        {
            var docsetPath = docset.DocsetPath;
            var config = docset.Config;

            _glob = CreateGlob(config);

            _fileNames = Directory.GetFiles(docsetPath, "*.*", SearchOption.AllDirectories)
                                  .Select(path => Path.GetRelativePath(docsetPath, path).Replace('\\', '/'))
                                  .ToHashSet(PathUtility.PathComparer);

            Redirections = RedirectionMap.Create(errorLog, docset, _glob);

            Files = new HashSet<Document>(GetFiles(docset, _glob).Concat(Redirections.Files));

            FilesWithFallback = GetFilesWithFallback(docset);
        }

        public bool GetActualFileName(string fileName, out string actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        private IReadOnlyList<Document> GetFiles(Docset docset, Func<string, bool> glob)
        {
            using (Progress.Start("Globbing files"))
            {
                var files = new ListBuilder<Document>();

                ParallelUtility.ForEach(_fileNames, file =>
                {
                    if (glob(file))
                    {
                        files.Add(Document.CreateFromFile(docset, file));
                    }
                });

                return files.ToList();
            }
        }

        private static Func<string, bool> CreateGlob(Config config)
        {
            return GlobUtility.CreateGlobMatcher(config.Files, config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }

        private HashSet<Document> GetFilesWithFallback(Docset docset)
        {
            var filesWithFallback = Files.ToHashSet();

            if (docset.FallbackDocset != null)
            {
                foreach (var document in GetFiles(docset.FallbackDocset, _glob))
                {
                    if (!_fileNames.Contains(document.FilePath))
                    {
                        filesWithFallback.Add(document);
                    }
                }
            }

            return filesWithFallback;
        }
    }
}
