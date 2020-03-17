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
        private readonly Output _output;
        private readonly ErrorLog _errorLog;
        private readonly ConcurrentDictionary<string, ConcurrentBag<FilePath>> _outputPathConflicts = new ConcurrentDictionary<string, ConcurrentBag<FilePath>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<FilePath, IReadOnlyList<string>>> _filesBySiteUrl = new ConcurrentDictionary<string, ConcurrentDictionary<FilePath, IReadOnlyList<string>>>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<string, FilePath> _filesByOutputPath = new ConcurrentDictionary<string, FilePath>(PathUtility.PathComparer);
        private readonly ConcurrentDictionary<FilePath, PublishItem> _publishItems = new ConcurrentDictionary<FilePath, PublishItem>();

        public PublishModelBuilder(string outputPath, Config config, Output output, ErrorLog errorLog)
        {
            _config = config;
            _output = output;
            _errorLog = errorLog;
            _outputPath = PathUtility.NormalizeFolder(outputPath);
        }

        public bool HasOutput(FilePath file)
        {
            return _publishItems.TryGetValue(file, out var item) && !item.HasError;
        }

        public bool TryAdd(FilePath file, PublishItem item)
        {
            _publishItems[file] = item;

            if (item.Path != null)
            {
                // Find output path conflicts
                if (!_filesByOutputPath.TryAdd(item.Path, file))
                {
                    if (_filesByOutputPath.TryGetValue(item.Path, out var existingFile) && existingFile != file)
                    {
                        _outputPathConflicts.GetOrAdd(item.Path, _ => new ConcurrentBag<FilePath>()).Add(file);
                    }
                    return false;
                }
            }

            var monikers = item.Monikers;
            if (monikers.Length == 0)
            {
                monikers = new[] { NonVersion };
            }
            _filesBySiteUrl.GetOrAdd(item.Url, _ => new ConcurrentDictionary<FilePath, IReadOnlyList<string>>()).TryAdd(file, monikers);

            return true;
        }

        public (List<Error> errors, PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var errors = new List<Error>();

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
                    errors.Add(Errors.UrlPath.PublishUrlConflict(siteUrl, files, conflictMoniker));
                    foreach (var conflictingFile in files.Keys)
                    {
                        DeleteOutput(conflictingFile);
                    }
                }
            }

            // Handle output path conflicts
            foreach (var (outputPath, conflict) in _outputPathConflicts)
            {
                var conflictingFiles = new HashSet<FilePath>();

                foreach (var conflictingFile in conflict)
                {
                    conflictingFiles.Add(conflictingFile);
                }

                if (_filesByOutputPath.TryRemove(outputPath, out var removed))
                {
                    conflictingFiles.Add(removed);
                }

                errors.Add(Errors.UrlPath.OutputPathConflict(outputPath, conflictingFiles));

                foreach (var conflictingFile in conflictingFiles)
                {
                    DeleteOutput(conflictingFile);
                }
            }

            // Delete files with errors from output
            foreach (var file in _errorLog.ErrorFiles)
            {
                DeleteOutput(file);
            }

            var publishItems = (
                from item in _publishItems.Values
                orderby item.Locale, item.Path, item.Url, item.RedirectUrl, item.MonikerGroup
                select item).ToArray();

            var monikerGroups = new Dictionary<string, string[]>(
                from item in _publishItems.Values
                let monikerGroup = item.MonikerGroup
                where !string.IsNullOrEmpty(monikerGroup)
                orderby monikerGroup
                group item by monikerGroup into g
                select new KeyValuePair<string, string[]>(g.Key, g.First().Monikers));

            var model = new PublishModel(
                _config.Name,
                _config.Product,
                _config.BasePath.ValueWithLeadingSlash,
                publishItems,
                monikerGroups);

            var fileManifests = _publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (errors, model, fileManifests);
        }

        private void DeleteOutput(FilePath file)
        {
            if (!_config.DryRun && _publishItems.TryGetValue(file, out var item))
            {
                item.HasError = true;

                if (item.Path != null && IsInsideOutputFolder(item.Path))
                    _output.Delete(item.Path, _config.Legacy);
            }
        }

        private bool IsInsideOutputFolder(string path)
        {
            var outputFilePath = PathUtility.NormalizeFolder(Path.Combine(_outputPath, path));
            return outputFilePath.StartsWith(_outputPath);
        }
    }
}
