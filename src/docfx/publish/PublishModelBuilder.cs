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
        private readonly ConcurrentDictionary<PublishItem, ConcurrentQueue<(FilePath, PublishItem, Action?)>> _outputsBySiteUrl
            = new ConcurrentDictionary<PublishItem, ConcurrentQueue<(FilePath, PublishItem, Action?)>>(new PublishItemComparer());

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

        public void Add(FilePath file, PublishItem item, Action? writeOutput = null)
        {
            _outputsBySiteUrl
                .GetOrAdd(item, _ => new ConcurrentQueue<(FilePath, PublishItem, Action?)>())
                .Enqueue((file, item, writeOutput));
        }

        public (List<Error> errors, PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var errors = new ConcurrentBag<Error>();

            ParallelUtility.ForEach(_outputsBySiteUrl, kvp => HandleConflicts(errors, kvp.Value));

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

        private void HandleConflicts(ConcurrentBag<Error> errors, ConcurrentQueue<(FilePath, PublishItem, Action?)> queue)
        {
            if (queue.Count == 1)
            {
                var (file, item, action) = queue.First();
                action?.Invoke();
                _publishItems.TryAdd(file, item);
            }
            else if (queue.Count > 1)
            {
                queue.TryDequeue(out var first);
                var (finalFile, finalItem, finalAction) = first;
                var outputPathConflictingFiles = new HashSet<FilePath>();
                var publishUrlConflicingFiles = new Dictionary<FilePath, IReadOnlyList<string>>();
                while (queue.TryDequeue(out var next))
                {
                    var (file, item, action) = next;

                    // handle output path conflicts
                    if (finalItem != null && finalFile != null && item.Path != null && OutputPathConflicts(finalItem?.Path, item.Path))
                    {
                        outputPathConflictingFiles.Add(finalFile);
                        outputPathConflictingFiles.Add(file);

                        // redirection file is preferred than source file
                        // otherwise, prefer the one based on FilePath
                        if (file.Origin == FileOrigin.Redirection ||
                            (finalFile != null && finalFile.Origin != FileOrigin.Redirection && file.CompareTo(finalFile) > 0))
                        {
                            finalFile = file;
                            finalItem = item;
                            finalAction = action;
                        }
                        continue;
                    }

                    // handle publish url conflicts
                    if (finalItem != null && finalFile != null)
                    {
                        publishUrlConflicingFiles.TryAdd(finalFile, finalItem.Monikers);
                        publishUrlConflicingFiles.TryAdd(file, item.Monikers);

                        var compareMoniker = CompareMonikerGroup(item.MonikerGroup, finalItem.MonikerGroup);
                        if (compareMoniker > 0
                            || (compareMoniker == 0
                                && (file.Origin == FileOrigin.Redirection
                                    || (finalFile != null && finalFile.Origin != FileOrigin.Redirection && file.CompareTo(finalFile) > 0))))
                        {
                            finalFile = file;
                            finalItem = item;
                            finalAction = action;
                        }
                    }
                }

                if (finalFile != null && finalItem != null)
                {
                    _publishItems.TryAdd(finalFile, finalItem);
                    finalAction?.Invoke();

                    if (finalItem.Path != null && outputPathConflictingFiles.Count > 1)
                    {
                        errors.Add(Errors.UrlPath.OutputPathConflict(finalItem.Path, outputPathConflictingFiles));
                    }

                    if (publishUrlConflicingFiles.Count > 1)
                    {
                        var conflictMoniker = publishUrlConflicingFiles
                            .SelectMany(file => file.Value)
                            .GroupBy(moniker => moniker)
                            .Where(group => group.Count() > 1)
                            .Select(group => group.Key)
                            .ToList();
                        errors.Add(Errors.UrlPath.PublishUrlConflict(finalItem.Url, publishUrlConflicingFiles, conflictMoniker));
                    }
                }
            }
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

        private static bool OutputPathConflicts(string? outputPath, string? otherOutputPath)
            => outputPath != null && otherOutputPath != null && PathUtility.PathComparer.Compare(outputPath, otherOutputPath) == 0;
    }
}
