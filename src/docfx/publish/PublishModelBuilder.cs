// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishModelBuilder
    {
        public const string NonVersion = "NONE_VERSION";

        private readonly string _outputPath;
        private readonly Config _config;
        private readonly ConcurrentDictionary<string, ConcurrentBag<Document>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<Document>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Document, IReadOnlyList<string>>> _filesBySiteUrl = new ConcurrentDictionary<string, ConcurrentDictionary<Document, IReadOnlyList<string>>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, Document> _filesByOutputPath = new ConcurrentDictionary<string, Document>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<Document, PublishItem> _publishItems = new ConcurrentDictionary<Document, PublishItem>();
        private readonly ConcurrentHashSet<Document> _excludedFiles = new ConcurrentHashSet<Document>();

        public PublishModelBuilder(string outputPath, Config config)
        {
            _config = config;
            _outputPath = PathUtility.NormalizeFolder(outputPath);
        }

        public void ExcludeFromOutput(Document file)
        {
            _excludedFiles.TryAdd(file);
        }

        public bool IsIncludedInOutput(Document file) => !_excludedFiles.Contains(file);

        public bool TryAdd(Document file, PublishItem item)
        {
            _publishItems[file] = item;

            if (item.Path != null)
            {
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
                monikers = new List<string> { PublishModelBuilder.NonVersion };
            }
            _filesBySiteUrl.GetOrAdd(item.Url, _ => new ConcurrentDictionary<Document, IReadOnlyList<string>>()).TryAdd(file, monikers);

            return true;
        }

        public (PublishModel, Dictionary<Document, PublishItem>) Build(Context context, bool legacy)
        {
            // Handle publish url conflicts
            foreach (var (siteUrl, files) in _filesBySiteUrl)
            {
                var conflictMoniker = files
                    .SelectMany(file => file.Value)
                    .GroupBy(moniker => moniker)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();

                if (conflictMoniker.Count != 0
                    || (files.Count > 1 && files.Any(file => file.Value.Contains(NonVersion))))
                {
                    context.ErrorLog.Write(Errors.PublishUrlConflict(siteUrl, files, conflictMoniker));
                    foreach (var conflictingFile in files.Keys)
                    {
                        HandleExcludedFile(context, conflictingFile, legacy);
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

                context.ErrorLog.Write(Errors.OutputPathConflict(outputPath, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    HandleExcludedFile(context, conflictingFile, legacy);
                }
            }

            // Handle files excluded from output
            foreach (var file in _excludedFiles.ToList())
            {
                if (_filesBySiteUrl.TryRemove(file.SiteUrl, out _))
                {
                    HandleExcludedFile(context, file, legacy);
                }
            }

            var model = new PublishModel
            {
                Name = _config.Name,
                Product = _config.Product,
                BasePath = _config.BasePath,
                Files = _publishItems.Values
                    .OrderBy(item => item.Locale)
                    .ThenBy(item => item.Path)
                    .ThenBy(item => item.Url)
                    .ThenBy(item => item.RedirectUrl)
                    .ThenBy(item => item.MonikerGroup)
                    .ToArray(),
                MonikerGroups = new SortedDictionary<string, IReadOnlyList<string>>(_publishItems.Values
                    .Where(item => !string.IsNullOrEmpty(item.MonikerGroup))
                    .GroupBy(item => item.MonikerGroup)
                    .ToDictionary(g => g.Key, g => g.First().Monikers)),
            };

            var fileManifests = _publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }

        private void HandleExcludedFile(Context context, Document file, bool legacy)
        {
            if (_publishItems.TryGetValue(file, out var item))
            {
                item.HasError = true;

                if (item.Path != null && IsInsideOutputFolder(item))
                    context.Output.Delete(item.Path, legacy);
            }
        }

        private bool IsInsideOutputFolder(PublishItem item)
        {
            var outputFilePath = PathUtility.NormalizeFolder(Path.Combine(_outputPath, item.Path));
            return outputFilePath.StartsWith(_outputPath);
        }
    }
}
