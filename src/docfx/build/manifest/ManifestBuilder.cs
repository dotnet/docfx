// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ManifestBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _siteUrlConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, List<(Document doc, List<string> monikers)>> _filesBySiteUrl = new ConcurrentDictionary<string, List<(Document doc, List<string> monikers)>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, Document> _filesByOutputPath = new ConcurrentDictionary<string, Document>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<Document, FileManifest> _manifest = new ConcurrentDictionary<Document, FileManifest>();
        private readonly ConcurrentBag<Document> _filesWithErrors = new ConcurrentBag<Document>();

        public void MarkError(Document file)
        {
            _filesWithErrors.Add(file);
        }

        public bool TryAdd(Document file, FileManifest manifest, List<string> monikers)
        {
            _manifest[file] = manifest;

            // TODO: see comments in Document.OutputPath.
            file.OutputPath = manifest.OutputPath;

            // Find publish url conflicts
            if (!_filesBySiteUrl.TryAdd(manifest.SiteUrl, new List<(Document doc, List<string> monikers)> { (file, monikers) })
                && _filesBySiteUrl.TryGetValue(manifest.SiteUrl, out var existingFiles))
            {
                foreach (var item in existingFiles)
                {
                    if (CheckMonikerConflict(item.monikers, monikers))
                    {
                        if (item.doc != file)
                        {
                            _siteUrlConflicts.GetOrAdd(manifest.SiteUrl, _ => new ConcurrentBag<Document>()).Add(file);
                        }
                        return false;
                    }
                }
                existingFiles.Add((file, monikers));
            }

            // Find output path conflicts
            if (!_filesByOutputPath.TryAdd(manifest.OutputPath, file))
            {
                if (_filesByOutputPath.TryGetValue(manifest.OutputPath, out var existingFile) && existingFile != file)
                {
                    _outputPathConflicts.GetOrAdd(manifest.OutputPath, _ => new ConcurrentBag<Document>()).Add(file);
                }
                return false;
            }

            return true;
        }

        public (FileManifest[], List<Document>) Build(Context context)
        {
            // Handle publish conflicts
            foreach (var (siteUrl, conflict) in _siteUrlConflicts)
            {
                var conflictingFiles = new HashSet<Document>();

                foreach (var conflictingFile in conflict)
                {
                    conflictingFiles.Add(conflictingFile);
                }

                if (_filesBySiteUrl.TryRemove(siteUrl, out var removed))
                {
                    conflictingFiles.UnionWith(removed.Select(item => item.doc).ToHashSet());
                }

                context.Report(Errors.PublishUrlConflict(siteUrl, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    if (_manifest.TryRemove(conflictingFile, out var manifest))
                    {
                        context.Delete(manifest.OutputPath);
                    }
                }
            }

            // Handle output path conflicts
            foreach (var (outputPath, conflict) in _outputPathConflicts)
            {
                var conflictingFiles = new HashSet<Document>();

                foreach (var conflictingFile in conflict)
                {
                    conflictingFiles.Add(conflictingFile);
                }

                if (_filesByOutputPath.TryRemove(outputPath, out var removed))
                {
                    conflictingFiles.Add(removed);
                }

                context.Report(Errors.OutputPathConflict(outputPath, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    if (_manifest.TryRemove(conflictingFile, out var manifest))
                    {
                        context.Delete(manifest.OutputPath);
                    }
                }
            }

            // Handle files with errors
            foreach (var file in _filesWithErrors)
            {
                if (_filesBySiteUrl.TryRemove(file.SiteUrl, out _))
                {
                    if (_manifest.TryRemove(file, out var manifest))
                    {
                        context.Delete(manifest.OutputPath);
                    }
                }
            }

            return (
                _manifest.Values.OrderBy(item => item.SourcePath).ToArray(),
                _manifest.Keys.OrderBy(item => item.FilePath).ToList());
        }

        private bool CheckMonikerConflict(List<string> existingMonikers, List<string> currentMonikers)
        {
            if (existingMonikers.Intersect(currentMonikers).Count() > 0 || existingMonikers.Concat(currentMonikers).Count() == 0)
            {
                return true;
            }
            return false;
        }
    }
}
