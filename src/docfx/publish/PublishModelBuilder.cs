// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishModelBuilder
    {
        private readonly object _lock = new object();
        private readonly Context _context;
        private readonly string _outputPath;
        private readonly Config _config;
        private readonly Output _output;
        private readonly ErrorLog _errorLog;
        private readonly ConcurrentDictionary<PublishItemKey, Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>> _outputConflicts
            = new ConcurrentDictionary<PublishItemKey, Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>>();

        private readonly Dictionary<FilePath, PublishItem> _publishItems = new Dictionary<FilePath, PublishItem>();

        public PublishModelBuilder(Context context, string outputPath, Config config, Output output, ErrorLog errorLog)
        {
            _context = context;
            _config = config;
            _output = output;
            _errorLog = errorLog;
            _outputPath = PathUtility.NormalizeFolder(outputPath);
        }

        public bool HasOutput(FilePath file)
        {
            return _publishItems.TryGetValue(file, out var item) && !item.HasError;
        }

        public void AddOrUpdate(FilePath file, PublishItem item, Action? writeOutput = null)
        {
            var newKey = new PublishItemKey(item, file);
            lock (_lock)
            {
                _outputConflicts.AddOrUpdate(
                    newKey,
                    key => Add(file, item, writeOutput),
                    (key, existingValue) => Update(file, item, writeOutput, existingValue, newKey));
            }
        }

        private Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)> Update(FilePath file, PublishItem item, Action? writeOutput, Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)> existingValue, PublishItemKey newKey)
        {
            var (existingFile, existingPublishItem, conflictingTYpe, conflictingFiles) = existingValue.Value;
            conflictingFiles.TryAdd(existingFile, existingPublishItem.Monikers);
            conflictingFiles.TryAdd(file, item.Monikers);

            if (PublishItemKey.OutputPathConflicts(newKey.OutputPath, existingPublishItem.Path))
            {
                // redirection file is preferred than source file
                // otherwise, prefer the one based on FilePath
                if (newKey.FilePath.Origin == FileOrigin.Redirection ||
                    (existingFile.Origin != FileOrigin.Redirection && newKey.FilePath.CompareTo(existingFile) > 0))
                {
                    if (!_config.DryRun)
                    {
                        writeOutput?.Invoke();
                    }
                    return new Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>(
                        () => (file, item, ConflictingType.OutputPathConflicts, conflictingFiles));
                }
                return new Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>(
                    () => (existingFile, existingPublishItem, ConflictingType.OutputPathConflicts, conflictingFiles));
            }
            else if (PublishItemKey.PublishUrlConflicts(newKey.SiteUrl, existingPublishItem.Url, newKey.Monikers, existingPublishItem.Monikers))
            {
                var compareMoniker = CompareMonikers(newKey.Monikers, existingPublishItem.Monikers);
                if (compareMoniker > 0)
                {
                    if (!_config.DryRun)
                    {
                        // delete output path from pervious moniker
                        if (existingPublishItem.Path != null && IsInsideOutputFolder(existingPublishItem.Path))
                            _output.Delete(existingPublishItem.Path, _config.Legacy);

                        writeOutput?.Invoke();
                    }
                    return new Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>(
                        () => (file, item, ConflictingType.PublishUrlConflicts, conflictingFiles));
                }
                return new Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>(
                    () => (existingFile, existingPublishItem, ConflictingType.PublishUrlConflicts, conflictingFiles));
            }
            return existingValue;
        }

        private int CompareMonikers(string[] monikers, string[] otherMonikers)
        {
            if (monikers.Length == 0)
            {
                if (otherMonikers.Length == 0)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if (otherMonikers.Length == 0)
            {
                return -1;
            }

            // monikers are sorted already
            var index = 0;
            while (index < monikers.Length)
            {
                if (index >= otherMonikers.Length)
                {
                    return 1;
                }
                var result = _context.MonikerProvider.Comparer.Compare(monikers[index], otherMonikers[index]);
                if (result != 0)
                {
                    return result;
                }
                index += 1;
            }
            if (otherMonikers.Length >= index)
            {
                return -1;
            }
            return 0;
        }

        private Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)> Add(FilePath file, PublishItem item, Action? writeOutput)
        {
            if (!_config.DryRun)
            {
                writeOutput?.Invoke();
            }
            return new Lazy<(FilePath, PublishItem, ConflictingType, ConcurrentDictionary<FilePath, IReadOnlyList<string>>)>(()
            => (file, item, ConflictingType.None, new ConcurrentDictionary<FilePath, IReadOnlyList<string>>()));
        }

        public (List<Error> errors, PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var errors = new List<Error>();

            // Handle conflicts
            foreach (var (key, conflict) in _outputConflicts)
            {
                var (existingFile, existingPublishItem, conflictingTYpe, conflictingFiles) = conflict.Value;
                var conflictMoniker = conflictingFiles
                    .SelectMany(file => file.Value)
                    .GroupBy(moniker => moniker)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToList();
                _publishItems.Add(existingFile, existingPublishItem);
                if (conflictingTYpe == ConflictingType.OutputPathConflicts)
                {
                    errors.Add(Errors.UrlPath.OutputPathConflict(key.OutputPath, conflictingFiles.Keys));
                }
                else if (conflictingTYpe == ConflictingType.PublishUrlConflicts)
                {
                    errors.Add(Errors.UrlPath.PublishUrlConflict(key.SiteUrl, conflictingFiles, conflictMoniker));
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
                if (item.Path != null && IsInsideOutputFolder(item.Path))
                    _output.Delete(item.Path, _config.Legacy);
            }
        }

        private bool IsInsideOutputFolder(string path)
        {
            var outputFilePath = PathUtility.NormalizeFolder(Path.Combine(_outputPath, path));
            return outputFilePath.StartsWith(_outputPath);
        }

        private enum ConflictingType
        {
            None,
            OutputPathConflicts,
            PublishUrlConflicts,
        }

        private class PublishItemKey : IEquatable<PublishItemKey>
        {
            public string[] Monikers { get; } = Array.Empty<string>();

            public string OutputPath { get; }

            public string SiteUrl { get; } = "";

            public FilePath FilePath { get; }

            public PublishItemKey(PublishItem item, FilePath file)
            {
                Debug.Assert(item != null);
                Monikers = item.Monikers;
                OutputPath = item.Path;
                SiteUrl = item.Url;
                FilePath = file;
            }

            public bool Equals(PublishItemKey? other)
            {
                if (other is null)
                {
                    return false;
                }

                return OutputPathConflicts(OutputPath, other.OutputPath) || PublishUrlConflicts(SiteUrl, other.SiteUrl, Monikers, other.Monikers);
            }

            public override bool Equals(object? obj)
                => Equals(obj as PublishItemKey);

            public override int GetHashCode()
                => PathUtility.PathComparer.GetHashCode(SiteUrl);

            public static bool PublishUrlConflicts(string siteUrl, string otherSiteUrl, string[] monikers, string[] otherMonikers)
                => PathUtility.PathComparer.Compare(siteUrl, otherSiteUrl) == 0
                    && (monikers.Length == 0
                    || otherMonikers.Length == 0
                    || monikers.Intersect(otherMonikers).Any());

            public static bool OutputPathConflicts(string? outputPath, string otherOutputPath)
                => outputPath != null && PathUtility.PathComparer.Compare(outputPath, otherOutputPath) == 0;
        }
    }
}
