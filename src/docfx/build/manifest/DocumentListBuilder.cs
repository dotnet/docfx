// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DocumentListBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _publishConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, List<(Document doc, List<string> monikers)>> _filesByUrl = new ConcurrentDictionary<string, List<(Document doc, List<string> monikers)>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, Document> _filesByOutputPath = new ConcurrentDictionary<string, Document>(PathUtility.PathComparer);

        public ICollection<Document> Build(Context context, IEnumerable<Document> filesWithErrors)
        {
            HandleConflicts(context, filesWithErrors);

            return _filesByUrl.Values.SelectMany(d => d.Select(item => item.doc)).ToList();
        }

        public bool TryAdd(Document file)
        {
            var monikersOfCurrentFile = file.Docset.MonikersProvider.GetMonikers(file);

            // Find publish url conflicts
            if (!_filesByUrl.TryAdd(file.SiteUrl, new List<(Document doc, List<string> monikers)> { (file, monikersOfCurrentFile) }))
            {
                _filesByUrl.TryGetValue(file.SiteUrl, out var existingFiles);
                foreach (var item in existingFiles)
                {
                    if (CheckMonikerConflict(item.monikers, monikersOfCurrentFile))
                    {
                        if (item.doc != file)
                        {
                            _publishConflicts.GetOrAdd(file.SiteUrl, _ => new ConcurrentBag<Document>()).Add(file);
                        }
                        return false;
                    }
                }
                existingFiles.Add((file, monikersOfCurrentFile));
            }

            // Find output path conflicts
            var outputPath = file.OutputPath;
            if (!_filesByOutputPath.TryAdd(outputPath, file))
            {
                if (_filesByOutputPath.TryGetValue(outputPath, out var existingFile) && existingFile != file)
                {
                    _outputPathConflicts.GetOrAdd(outputPath, _ => new ConcurrentBag<Document>()).Add(file);
                }
                return false;
            }

            return true;
        }

        private bool CheckMonikerConflict(List<string> existingMonikers, List<string> currentMonikers)
        {
            if (existingMonikers.Intersect(currentMonikers).Count() > 0 || existingMonikers.Concat(currentMonikers).Count() == 0)
            {
                return true;
            }
            return false;
        }

        private void HandleConflicts(Context context, IEnumerable<Document> filesWithErrors)
        {
            // Handle publish conflicts
            foreach (var (siteUrl, conflict) in _publishConflicts)
            {
                var conflictingFiles = new HashSet<Document>();

                foreach (var conflictingFile in conflict)
                {
                    conflictingFiles.Add(conflictingFile);
                }

                if (_filesByUrl.TryRemove(siteUrl, out var removed))
                {
                    conflictingFiles.UnionWith(removed.Select(item => item.doc).ToHashSet());
                }

                context.Report(Errors.PublishUrlConflict(siteUrl, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    context.Delete(conflictingFile.OutputPath);
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
                    context.Delete(conflictingFile.OutputPath);
                }
            }

            // Handle files with errors
            foreach (var file in filesWithErrors)
            {
                if (_filesByUrl.TryRemove(file.SiteUrl, out _))
                {
                    context.Delete(file.OutputPath);
                }
            }
        }
    }
}
