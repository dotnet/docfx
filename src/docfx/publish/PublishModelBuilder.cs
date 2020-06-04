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
        private readonly Context _context;
        private readonly ConcurrentDictionary<PublishItem, Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>> _outputsByPublishItem
            = new ConcurrentDictionary<PublishItem, Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>>(new PublishItemComparer());

        private readonly Dictionary<FilePath, PublishItem> _publishItems = new Dictionary<FilePath, PublishItem>();
        private readonly ListBuilder<(PublishItem, Document)> _outputMapping = new ListBuilder<(PublishItem, Document)>();

        public PublishModelBuilder(
            string outputPath,
            Context context)
        {
            _outputPath = PathUtility.NormalizeFolder(outputPath);
            _context = context;
            BuildCore();
        }

        public bool HasOutput(FilePath file)
        {
            return _publishItems.TryGetValue(file, out var item) && !item.HasError;
        }

        private void BuildCore()
        {
            var builder = new ListBuilder<(PublishItem item, Document file)>();
            ParallelUtility.ForEach(
                    _context.ErrorLog,
                    _context.RedirectionProvider.Files
                         .Concat(_context.BuildScope.GetFiles(ContentType.Resource))
                         .Concat(_context.BuildScope.GetFiles(ContentType.Page))
                         .Concat(_context.TocMap.GetFiles()),
                    AddToPublishMapping);

            // resolve output path conflicts
            var groupByOutputPath = builder.ToList().GroupBy(x => x.item.Path, PathUtility.PathComparer);
            var temp = groupByOutputPath.Where(g => g.Count() == 1).SelectMany(g => g)
                .Concat(groupByOutputPath.Where(g => g.Count() > 1).Select(g => ResolveOutputPathConflicts(g)));

            // resolve publish url conflicts
            var groupByPublishUrl = temp.GroupBy(x => x.Item1, new PublishItemComparer());
            return groupByPublishUrl.Where(g => g.Count() == 1).SelectMany(g => g)
                .Concat(groupByPublishUrl.Where(g => g.Count() > 1).Select(g => ResolvePublishUrlConflicts(g)))
                .ToDictionary(g => g.Key.Path, g => ResolvePublishUrlConflicts(g.ToArray()));
        }

        private (PublishItem, Document) ResolveOutputPathConflicts((PublishItem item, Document file)[] conflicts)
        {
            // no conflicts
            if (conflicts.Length == 1)
            {
                return conflicts.First();
            }

            _context.ErrorLog.Write(Errors.UrlPath.OutputPathConflict(conflicts.First().item.Path!, conflicts.Select(x => x.file.FilePath)));

            var publishItem = conflicts.First().item;

            // redirection file is preferred than source file
            // otherwise, prefer the one based on FilePath
            var redirectionFiles = conflicts.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                return (publishItem, redirectionFiles.OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
            }

            return (publishItem, conflicts.Select(x => x.file).OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
        }

        private (PublishItem, Document) ResolvePublishUrlConflicts((PublishItem item, Document file)[] conflicts)
        {
            var chosenMonikerGroup = conflicts.OrderBy(x => x.item.MonikerGroup, PathUtility.PathComparer).First().item.MonikerGroup;
            var itemsWithChosenMonikerGroup = conflicts.Where(x => x.item.MonikerGroup == chosenMonikerGroup);
            if (itemsWithChosenMonikerGroup.Count() == 1)
            {
                return itemsWithChosenMonikerGroup.First();
            }

            var publishItem = conflicts.First().item;
            var redirectionFiles = itemsWithChosenMonikerGroup.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                return (publishItem, redirectionFiles.OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
            }

            return (publishItem, itemsWithChosenMonikerGroup.Select(x => x.file).OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
            //var conflictMoniker = conflicts
            //    .SelectMany(file => file.Value.Item2)
            //    .GroupBy(moniker => moniker)
            //    .Where(group => group.Count() > 1)
            //    .Select(group => group.Key)
            //    .ToList();

            //var conflicts = conflictingFiles.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Item2);
            //errors.Add(Errors.UrlPath.PublishUrlConflict(item.Url, conflicts, conflictMoniker));
        }

        private void AddToPublishMapping(FilePath path)
        {
            var file = _context.DocumentProvider.GetDocument(path);
            var (monikerErrors, monikers) = _context.MonikerProvider.GetFileLevelMonikers(path);
            _context.ErrorLog.Write(monikerErrors);
            var publishPath = "";
            switch (file.ContentType)
            {
                case ContentType.Redirection:
                    publishPath = _context.Config.Legacy ? _context.DocumentProvider.GetOutputPath(path) : null;
                    break;
                case ContentType.Page:
                case ContentType.TableOfContents:
                    publishPath = _context.DocumentProvider.GetOutputPath(path);
                    break;
                case ContentType.Resource:
                    publishPath = _context.DocumentProvider.GetOutputPath(path);

                    if (!_context.Config.CopyResources &&
                        _context.Input.TryGetPhysicalPath(path, out var physicalPath))
                    {
                        publishPath = PathUtility.NormalizeFile(Path.GetRelativePath(_context.Output.OutputPath, physicalPath));
                    }
                    break;
            }
            var publishItem = new PublishItem(
                file.SiteUrl,
                publishPath,
                _context.SourceMap.GetOriginalFilePath(path) ?? path.Path,
                _context.BuildOptions.Locale,
                monikers,
                _context.MonikerProvider.GetConfigMonikerRange(path),
                file.ContentType,
                file.Mime);
            _outputMapping.Add((publishItem, file));
        }

        public void Add(FilePath file, PublishItem item, Action? writeOutput = null)
        {
            var (addedItem, writeLock, _, _, _) = _outputsByPublishItem
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
                                if (existingItem != null && existingFile != null)
                                {
                                    return (existingItem, writeLock, existingFile, conflicts, ConflictingType.OutputPathConflicts);
                                }
                                throw new InvalidOperationException();
                            });
                    }

                    if (PublishItemComparer.PublishUrlEquals(item, existingItem))
                    {
                        return new Lazy<(PublishItem, object, FilePath, ConcurrentDictionary<FilePath, (string?, IReadOnlyList<string>)>, ConflictingType)>(
                            () =>
                            {
                                var compareMoniker = CompareMonikerGroup(item.MonikerGroup, existingItem.MonikerGroup);
                                if (compareMoniker > 0
                                    || (compareMoniker == 0
                                        && (file.Origin == FileOrigin.Redirection
                                            || (existingFile != null && existingFile.Origin != FileOrigin.Redirection && file.CompareTo(existingFile) > 0))))
                                {
                                    return (item, writeLock, file, conflicts, ConflictingType.PublishUrlConflicts);
                                }
                                if (existingItem != null && existingFile != null)
                                {
                                    return (existingItem, writeLock, existingFile, conflicts, ConflictingType.PublishUrlConflicts);
                                }
                                throw new InvalidOperationException();
                            });
                    }
                    throw new InvalidOperationException();
                }).Value;

            if (addedItem == item)
            {
                lock (writeLock)
                {
                    if (_outputsByPublishItem.TryGetValue(item, out var current)
                        && item == current.Value.Item1
                        && !_context.Config.DryRun)
                    {
                        writeOutput?.Invoke();
                    }
                }
            }
        }

        public (List<Error> errors, PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var errors = new List<Error>();

            foreach (var (_, conflict) in _outputsByPublishItem)
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
                        if (conflictingOutputPath != item.Path && !_context.Config.DryRun && conflictingOutputPath != null && IsInsideOutputFolder(conflictingOutputPath))
                        {
                            _context.Output.Delete(conflictingOutputPath, _context.Config.Legacy);
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
            foreach (var file in _context.ErrorLog.ErrorFiles)
            {
                DeleteOutput(file);
            }

            foreach (var (filePath, publishItem) in _publishItems)
            {
                if (!publishItem.HasError)
                {
                    Telemetry.TrackBuildFileTypeCount(filePath, publishItem);
                    _context.ContentValidator.ValidateManifest(filePath, publishItem);
                }
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
                _context.Config.Name,
                _context.Config.Product,
                _context.Config.BasePath.ValueWithLeadingSlash,
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
            if (!_context.Config.DryRun && _publishItems.TryGetValue(file, out var item))
            {
                item.HasError = true;
                if (item.Path != null && IsInsideOutputFolder(item.Path))
                {
                    _context.Output.Delete(item.Path, _context.Config.Legacy);
                }
            }
        }

        private bool IsInsideOutputFolder(string path)
        {
            var outputFilePath = PathUtility.NormalizeFolder(Path.Combine(_outputPath, path));
            return outputFilePath.StartsWith(_outputPath);
        }
    }
}
