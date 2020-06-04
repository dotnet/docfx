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
        private readonly Context _context;
        private readonly Dictionary<PublishItem, FilePath> _outputMapping;
        private readonly ConcurrentDictionary<FilePath, PublishItem> _publishItems;

        public PublishModelBuilder(Context context)
        {
            _context = context;
            _outputMapping = Initialize();
            _publishItems = new ConcurrentDictionary<FilePath, PublishItem>(_outputMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));
        }

        public IEnumerable<FilePath> GetFiles()
        {
            return _publishItems.Keys;
        }

        public PublishItem GetPublishItem(FilePath file)
        {
            return _publishItems[file];
        }

        public bool HasOutput(FilePath file)
        {
            return _publishItems.TryGetValue(file, out var item) && !item.HasError;
        }

        public (PublishModel, Dictionary<FilePath, PublishItem>) Build()
        {
            var publishItems = (
                   from item in _publishItems.Values
                   orderby item.Locale, item.Path, item.Url, item.RedirectUrl, item.MonikerGroup
                   select item).ToArray();

            var monikerGroups = new Dictionary<string, MonikerList>(
                from item in _publishItems.Values
                let monikerGroup = item.MonikerGroup
                where !string.IsNullOrEmpty(monikerGroup)
                orderby monikerGroup
                group item by monikerGroup into g
                select new KeyValuePair<string, MonikerList>(g.Key, g.First().Monikers));

            var model = new PublishModel(
                _context.Config.Name,
                _context.Config.Product,
                _context.Config.BasePath.ValueWithLeadingSlash,
                publishItems,
                monikerGroups);

            var fileManifests = _publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }

        public void ExcludeErrorFiles()
        {
            foreach (var file in _context.ErrorLog.ErrorFiles)
            {
                DeleteOutput(file);
            }
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
            var outputFilePath = PathUtility.NormalizeFolder(Path.Combine(_context.Output.OutputPath, path));
            return outputFilePath.StartsWith(PathUtility.NormalizeFolder(_context.Output.OutputPath));
        }

        private Dictionary<PublishItem, FilePath> Initialize()
        {
            var builder = new ListBuilder<(PublishItem item, Document file)>();
            var files = _context.RedirectionProvider.Files
                         .Concat(_context.BuildScope.GetFiles(ContentType.Resource))
                         .Concat(_context.BuildScope.GetFiles(ContentType.Page))
                         .Concat(_context.TocMap.GetFiles());
            using (Progress.Start("Building publish map"))
            {
                ParallelUtility.ForEach(
                    _context.ErrorLog,
                    files,
                    file => AddToPublishMapping(builder, file));
            }

            // resolve output path conflicts
            var groupByOutputPath = builder.ToList().GroupBy(x => x.item.Path, PathUtility.PathComparer);
            var temp = groupByOutputPath.Where(g => g.Count() == 1).SelectMany(g => g)
                .Concat(groupByOutputPath.Where(g => g.Count() > 1).Select(g => ResolveOutputPathConflicts(g.ToArray())));

            // resolve publish url conflicts
            var groupByPublishUrl = temp.GroupBy(x => x.Item1, new PublishItemComparer());
            var result = groupByPublishUrl.Where(g => g.Count() == 1).SelectMany(g => g)
                .Concat(groupByPublishUrl.Where(g => g.Count() > 1).Select(g => ResolvePublishUrlConflicts(g.ToArray())));
            return result.ToDictionary(g => g.Item1, g => g.Item2.FilePath);
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
            var publishItem = conflicts.First().item;

            var conflictMonikers = conflicts
                .SelectMany(x => x.item.Monikers)
                .GroupBy(moniker => moniker)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var conflictingFiles = conflicts.ToDictionary(x => x.file.FilePath, x => x.item.Monikers);
            _context.ErrorLog.Write(Errors.UrlPath.PublishUrlConflict(publishItem.Url, conflictingFiles, conflictMonikers));

            var lastestMonikerGroup = conflicts.OrderBy(x => x.item.MonikerGroup, PathUtility.PathComparer).First().item.MonikerGroup;
            var itemsWithChosenMonikerGroup = conflicts.Where(x => x.item.MonikerGroup == lastestMonikerGroup);
            if (itemsWithChosenMonikerGroup.Count() == 1)
            {
                return itemsWithChosenMonikerGroup.First();
            }

            var redirectionFiles = itemsWithChosenMonikerGroup.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                return (publishItem, redirectionFiles.OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
            }

            return (publishItem, itemsWithChosenMonikerGroup.Select(x => x.file).OrderBy(x => x.FilePath.Path, PathUtility.PathComparer).First());
        }

        private void AddToPublishMapping(ListBuilder<(PublishItem item, Document file)> outputMapping, FilePath path)
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

            // TODO: Add experimental and experiment_id to publish item for TOC
            var publishItem = new PublishItem(
                file.SiteUrl,
                publishPath,
                _context.SourceMap.GetOriginalFilePath(path) ?? path.Path,
                _context.BuildOptions.Locale,
                monikers,
                _context.MonikerProvider.GetConfigMonikerRange(path),
                file.ContentType,
                file.Mime);
            outputMapping.Add((publishItem, file));
        }
    }
}
