// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ManifestBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _siteUrlConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentBag<(Document doc, List<string> monikers)>> _filesBySiteUrl = new ConcurrentDictionary<string, ConcurrentBag<(Document doc, List<string> monikers)>>(PathUtility.PathComparer);
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

            if (_filesBySiteUrl.TryGetValue(manifest.SiteUrl, out var existingFiles))
            {
                existingFiles.Add((file, monikers));
            }
            else
            {
                _filesBySiteUrl.TryAdd(manifest.SiteUrl, new ConcurrentBag<(Document doc, List<string> monikers)> { (file, monikers) });
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
            foreach (var (siteUrl, files) in _filesBySiteUrl)
            {
                var hasConflict = false;
                var uniqueFiles = files.GroupBy(file => file.doc).ToDictionary(group => group.Key, group => group.First().monikers).ToList();
                for (var i = 0; i < uniqueFiles.Count; i++)
                {
                    var firstMonikers = uniqueFiles[i].Value;
                    if (uniqueFiles.Skip(i + 1).Any(file => CheckMonikerConflict(firstMonikers, file.Value)))
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (hasConflict)
                {
                    var conflictingFiles = uniqueFiles.Select(file => file.Key).ToList();
                    context.Report(Errors.PublishUrlConflict(siteUrl, conflictingFiles));

                    foreach (var conflictingFile in conflictingFiles)
                    {
                        if (_manifest.TryRemove(conflictingFile, out var manifest))
                        {
                            context.Delete(manifest.OutputPath);
                        }
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
            if (existingMonikers.Intersect(currentMonikers, StringComparer.OrdinalIgnoreCase).Any() || (!existingMonikers.Any() && !currentMonikers.Any()))
            {
                return true;
            }
            return false;
        }
    }
}
