// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishModelBuilder
    {
        private readonly string _outputPath;
        private readonly Config _config;
        private readonly Output _output;
        private readonly ErrorLog _errorLog;
        private readonly ConcurrentDictionary<PublishItem, Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>> _outputsBySiteUrl
            = new ConcurrentDictionary<PublishItem, Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>>(new PublishItemComparer());

        private readonly Dictionary<FilePath, PublishItem> _publishItems = new Dictionary<FilePath, PublishItem>();

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

        public void Add(FilePath file, PublishItem item, Action? writeOutput = null)
        {
            var (addedItem, _, writeLock, _, _) = _outputsBySiteUrl
                .AddOrUpdate(
                item,
                key => new Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>(
                    () => (item, new object(), file, new ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>(), ConflictingType.None)),
                (key, existing) =>
                {
                    var (existingItem, writeLock, existingFile, conflicts, _) = existing.Value;
                    conflicts.TryAdd(file, (item.Path, item.Monikers));
                    conflicts.TryAdd(existingFile, (existingItem.Path, existingItem.Monikers));

                    if (PublishItemComparer.OutputPathEquals(item, existingItem))
                    {
                        return new Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>(
                            () =>
                            {
                                // redirection file is preferred than source file
                                // otherwise, prefer the one based on FilePath
                                if (file.Origin == FileOrigin.Redirection ||
                                    (existingFile != null && existingFile.Origin != FileOrigin.Redirection && file.CompareTo(existingFile) > 0))
                                {
                                    return (item, writeLock, file, conflicts, ConflictingType.OutputPathConflicts);
                                }
                                else if (existingItem != null && writeLock != null && existingFile != null)
                                {
                                    return (existingItem, writeLock, existingFile, conflicts, ConflictingType.OutputPathConflicts);
                                }
                                return default;
                            });
                    }

                    return new Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>(
                        () =>
                        {
                            if (PublishItemComparer.PublishUrlEquals(item, existingItem))
                            {
                                var compareMoniker = CompareMonikerGroup(item.MonikerGroup, existingItem.MonikerGroup);
                                if (compareMoniker > 0
                                    || (compareMoniker == 0
                                        && (file.Origin == FileOrigin.Redirection
                                            || (existingFile != null && existingFile.Origin != FileOrigin.Redirection && file.CompareTo(existingFile) > 0))))
                                {
                                    return (item, writeLock, file, conflicts, ConflictingType.PublishUrlConflicts);
                                }
                            }
                            else if (existingItem != null && writeLock != null && existingFile != null)
                            {
                                return (existingItem, writeLock, existingFile, conflicts, ConflictingType.PublishUrlConflicts);
                            }
                            return default;
                        });
                }).Value;

            if (addedItem == item)
            {
                lock (writeLock)
                {
                    if (_outputsBySiteUrl.TryGetValue(item, out var current)
                        && item == current.Value.Item1
                        && !_config.DryRun)
                    {
                        writeOutput?.Invoke();
                    }
                }
            }
        }

        public (List<Error> errors, PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var errors = new List<Error>();

            foreach (var (_, conflict) in _outputsBySiteUrl)
            {
                var (item, _, file, conflictingFiles, conflictingTYpe) = conflict.Value;
                _publishItems.Add(file, item);

                if (item.Path != null && conflictingTYpe == ConflictingType.OutputPathConflicts)
                {
                    errors.Add(Errors.UrlPath.OutputPathConflict(item.Path, conflictingFiles.Keys));
                }
                else if (conflictingTYpe == ConflictingType.PublishUrlConflicts)
                {
                    foreach (var (_, (conflictingOutputPath, _)) in conflictingFiles)
                    {
                        if (conflictingOutputPath != item.Path && !_config.DryRun && conflictingOutputPath != null && IsInsideOutputFolder(conflictingOutputPath))
                        {
                            _output.Delete(conflictingOutputPath, _config.Legacy);
                        }
                    }

                    var conflictMoniker = conflictingFiles
                        .SelectMany(file => file.Value.Item2)
                        .GroupBy(moniker => moniker)
                        .Where(group => group.Count() > 1)
                        .Select(group => group.Key)
                        .ToList();

                    var conflicts = conflictingFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item2);
                    errors.Add(Errors.UrlPath.PublishUrlConflict(item.Url, conflicts, conflictMoniker));
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

            return (errors.ToList(), model, fileManifests);
        }

        private enum ConflictingType
        {
            None,
            OutputPathConflicts,
            PublishUrlConflicts,
        }

        private int CompareMonikerGroup(string? monikerGroup, string? otherMonikerGroup)
        {
            if (monikerGroup is null)
            {
                if (otherMonikerGroup != null)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else if (otherMonikerGroup is null)
            {
                return -1;
            }
            return PathUtility.PathComparer.Compare(monikerGroup, otherMonikerGroup);
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
