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
        private readonly Docset _fallbackDocset;
        private readonly Docset _docset;

        /// <summary>
        /// Gets all the files to build, including redirections and fallback files.
        /// </summary>
        public HashSet<Document> Files { get; }

        public RedirectionMap Redirections { get; }

        public BuildScope(ErrorLog errorLog, Docset docset, Docset fallbackDocset, TemplateEngine templateEngine)
        {
            var config = docset.Config;

            _docset = docset;
            _fallbackDocset = fallbackDocset;
            _glob = CreateGlob(config);
            _templateEngine = templateEngine;

            var (fileNames, files) = GetFiles(docset, _glob);

            var fallbackFiles = fallbackDocset != null
                ? GetFiles(fallbackDocset, CreateGlob(fallbackDocset.Config)).files
                : Enumerable.Empty<Document>();

            _fileNames = fileNames;

            Files = files.Concat(fallbackFiles.Where(file => !_fileNames.Contains(file.FilePath.Path))).ToHashSet();

            Redirections = RedirectionMap.Create(errorLog, docset, _glob, templateEngine, Files);

            Files.UnionWith(Redirections.Files);
        }

        public bool GetActualFileName(string fileName, out string actualFileName)
        {
            return _fileNames.TryGetValue(fileName, out actualFileName);
        }

        public Docset GetFallbackDocset(Docset docset)
            => docset == _docset || docset == _fallbackDocset ? _fallbackDocset : null;

        public bool TryResolveDocset(Docset docset, string file, out (Docset resolvedDocset, FileOrigin fileOrigin) resolved)
        {
            docset = docset == _fallbackDocset ? _docset : docset;
            var fallbackDocset = GetFallbackDocset(docset);
            resolved = default;

            // resolve from current docset
            if (File.Exists(Path.Combine(docset.DocsetPath, file)))
            {
                resolved.resolvedDocset = docset;
                resolved.fileOrigin = FileOrigin.Current;
                return true;
            }

            // resolve from fallback docset
            if (fallbackDocset != null && File.Exists(Path.Combine(fallbackDocset.DocsetPath, file)))
            {
                resolved.resolvedDocset = fallbackDocset;
                resolved.fileOrigin = FileOrigin.Fallback;
                return true;
            }

            return false;
        }

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
                        files.Add(Document.Create(docset, file, _templateEngine, GetOrigin(docset)));
                    }
                });

                return (fileNames, files.ToList());
            }
        }

        private FileOrigin GetOrigin(Docset docset)
            => docset == _fallbackDocset ? FileOrigin.Fallback : FileOrigin.Current;

        private static Func<string, bool> CreateGlob(Config config)
        {
            return GlobUtility.CreateGlobMatcher(
                config.Files,
                config.Exclude.Concat(Config.DefaultExclude).ToArray());
        }
    }
}
