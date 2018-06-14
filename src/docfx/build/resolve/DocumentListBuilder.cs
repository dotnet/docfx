// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class DocumentListBuilder
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _publishConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>();
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>();
        private readonly ConcurrentDictionary<string, Document> _filesByUrl = new ConcurrentDictionary<string, Document>();
        private readonly ConcurrentDictionary<string, Document> _filesByOutputPath = new ConcurrentDictionary<string, Document>();

        public List<Document> Build(Context context)
        {
            HandleConflicts(context);

            return _filesByUrl.Values.OrderBy(d => d.OutputPath).ToList();
        }

        public bool TryAdd(Document file)
        {
            // Find publish url conflicts
            if (!_filesByUrl.TryAdd(file.SiteUrl, file))
            {
                if (_filesByUrl.TryGetValue(file.SiteUrl, out var existingFile) && existingFile != file)
                {
                    _publishConflicts.GetOrAdd(file.SiteUrl, _ => new ConcurrentBag<Document>()).Add(file);
                }
                return false;
            }

            // Find output path conflicts
            if (!_filesByOutputPath.TryAdd(file.OutputPath, file))
            {
                if (_filesByOutputPath.TryGetValue(file.OutputPath, out var existingFile) && existingFile != file)
                {
                    _outputPathConflicts.GetOrAdd(file.OutputPath, _ => new ConcurrentBag<Document>()).Add(file);
                }
                return false;
            }

            return true;
        }

        private void HandleConflicts(Context context)
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
                    conflictingFiles.Add(removed);
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
        }
    }
}
