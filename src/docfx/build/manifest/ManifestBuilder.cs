// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class ManifestBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Document, List<string>>> _filesBySiteUrl = new ConcurrentDictionary<string, ConcurrentDictionary<Document, List<string>>>(PathUtility.PathComparer);
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

            // Find output path conflicts
            if (!_filesByOutputPath.TryAdd(manifest.OutputPath, file))
            {
                if (_filesByOutputPath.TryGetValue(manifest.OutputPath, out var existingFile) && existingFile != file)
                {
                    _outputPathConflicts.GetOrAdd(manifest.OutputPath, _ => new ConcurrentBag<Document>()).Add(file);
                }
                return false;
            }

            if (monikers.Count == 0)
            {
                // TODO: report a warning if there are multiple files published to same url, one of them have no version
                monikers = new List<string> { "NONE_VERSION" };
            }
            _filesBySiteUrl.GetOrAdd(manifest.SiteUrl, _ => new ConcurrentDictionary<Document, List<string>>()).TryAdd(file, monikers);

            return true;
        }

        public Dictionary<Document, FileManifest> Build(Context context)
        {
            // Handle publish url conflicts
            // TODO: Report more detail info for url conflict
            foreach (var (siteUrl, files) in _filesBySiteUrl)
            {
                var conflictMoniker = files
                    .SelectMany(file => file.Value)
                    .GroupBy(moniker => moniker)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);
                if (conflictMoniker.Count() > 0)
                {
                    context.Report(Errors.PublishUrlConflict(siteUrl, files.Keys, conflictMoniker));

                    foreach (var conflictingFile in files.Keys)
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

            return _manifest.ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
