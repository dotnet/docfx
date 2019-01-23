// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishModelBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Document, List<string>>> _filesBySiteUrl = new ConcurrentDictionary<string, ConcurrentDictionary<Document, List<string>>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, Document> _filesByOutputPath = new ConcurrentDictionary<string, Document>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<Document, PublishItem> _publishItems = new ConcurrentDictionary<Document, PublishItem>();
        private readonly ConcurrentBag<Document> _filesWithErrors = new ConcurrentBag<Document>();

        public void MarkError(Document file)
        {
            _filesWithErrors.Add(file);
        }

        public bool TryAdd(Document file, PublishItem item)
        {
            _publishItems[file] = item;

            if (item.Path != null)
            {
                // TODO: see comments in Document.OutputPath.
                file.OutputPath = item.Path;

                // Find output path conflicts
                if (!_filesByOutputPath.TryAdd(item.Path, file))
                {
                    if (_filesByOutputPath.TryGetValue(item.Path, out var existingFile) && existingFile != file)
                    {
                        _outputPathConflicts.GetOrAdd(item.Path, _ => new ConcurrentBag<Document>()).Add(file);
                    }
                    return false;
                }
            }

            var monikers = item.Monikers;
            if (monikers.Count == 0)
            {
                // TODO: report a warning if there are multiple files published to same url, one of them have no version
                monikers = new List<string> { "NONE_VERSION" };
            }
            _filesBySiteUrl.GetOrAdd(item.Url, _ => new ConcurrentDictionary<Document, List<string>>()).TryAdd(file, monikers);

            return true;
        }

        public (PublishModel, Dictionary<Document, PublishItem>) Build(Context context)
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
                    context.Report.Write(Errors.PublishUrlConflict(siteUrl, files.Keys, conflictMoniker));

                    foreach (var conflictingFile in files.Keys)
                    {
                        if (_publishItems.TryRemove(conflictingFile, out var item) && item.Path != null)
                        {
                            context.Output.Delete(item.Path);
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

                context.Report.Write(Errors.OutputPathConflict(outputPath, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    if (_publishItems.TryRemove(conflictingFile, out var item) && item.Path != null)
                    {
                        context.Output.Delete(item.Path);
                    }
                }
            }

            // Handle files with errors
            foreach (var file in _filesWithErrors)
            {
                if (_filesBySiteUrl.TryRemove(file.SiteUrl, out _))
                {
                    if (_publishItems.TryRemove(file, out var item) && item.Path != null)
                    {
                        context.Output.Delete(item.Path);
                    }
                }
            }

            var model = new PublishModel
            {
                Files = _publishItems.Values
                    .OrderBy(item => item.Locale)
                    .ThenBy(item => item.Path)
                    .ThenBy(item => item.Url)
                    .ThenBy(item => item.RedirectUrl)
                    .ToArray(),
            };

            var fileManifests = _publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }
    }
}
