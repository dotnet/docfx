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
        private readonly Config _config;
        private readonly ErrorLog _errorLog;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly Input _input;
        private readonly SourceMap _sourceMap;
        private readonly string _locale;
        private readonly string _outputPath;
        private readonly ContentValidator _contentValidator;

        private Dictionary<PublishItem, FilePath> _outputMapping = new Dictionary<PublishItem, FilePath>();
        private ConcurrentDictionary<FilePath, PublishItem> _publishItems = new ConcurrentDictionary<FilePath, PublishItem>();

        public PublishModelBuilder(
            Config config,
            ErrorLog errorLog,
            BuildScope buildScope,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            Input input,
            SourceMap sourceMap,
            BuildOptions buildOptions,
            ContentValidator contentValidator)
        {
            _config = config;
            _errorLog = errorLog;
            _buildScope = buildScope;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _input = input;
            _sourceMap = sourceMap;
            _locale = buildOptions.Locale;
            _outputPath = PathUtility.NormalizeFolder(buildOptions.OutputPath);
            _contentValidator = contentValidator;
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

        public (PublishModel, Dictionary<FilePath, PublishItem>) Build(TableOfContentsMap tocMap)
        {
            var builder = new ListBuilder<(PublishItem item, Document file)>();
            var files = _redirectionProvider.Files
                         .Concat(_buildScope.GetFiles(ContentType.Resource))
                         .Concat(_buildScope.GetFiles(ContentType.Page))
                         .Concat(tocMap.GetFiles());
            using (Progress.Start("Building publish map"))
            {
                ParallelUtility.ForEach(
                    _errorLog,
                    files,
                    file => AddToPublishMapping(builder, file));
            }

            // resolve output path conflicts
            var publishMap = builder.ToList();
            var groupByOutputPath = publishMap.Where(x => x.item.Path != null).GroupBy(x => x.item.Path, PathUtility.PathComparer);
            var temp = publishMap.Where(x => x.item.Path is null)
                .Concat(groupByOutputPath.Where(g => g.Count() == 1).SelectMany(g => g))
                .Concat(groupByOutputPath.Where(g => g.Count() > 1).Select(g => ResolveOutputPathConflicts(g.ToArray())));

            // resolve publish url conflicts
            var groupByPublishUrl = temp.GroupBy(x => x.Item1, new PublishItemComparer());
            _outputMapping = groupByPublishUrl.Where(g => g.Count() == 1).SelectMany(g => g)
                            .Concat(groupByPublishUrl.Where(g => g.Count() > 1).Select(g => ResolvePublishUrlConflicts(g.ToArray())))
                            .ToDictionary(g => g.Item1, g => g.Item2.FilePath);
            _publishItems = new ConcurrentDictionary<FilePath, PublishItem>(_outputMapping.ToDictionary(kvp => kvp.Value, kvp => kvp.Key));

            foreach (var (filePath, publishItem) in _publishItems)
            {
                Telemetry.TrackBuildFileTypeCount(filePath, publishItem);
                _contentValidator.ValidateManifest(filePath, publishItem);
            }

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
                _config.Name,
                _config.Product,
                _config.BasePath.ValueWithLeadingSlash,
                publishItems,
                monikerGroups);

            var fileManifests = _publishItems.ToDictionary(item => item.Key, item => item.Value);

            return (model, fileManifests);
        }

        private (PublishItem, Document) ResolveOutputPathConflicts((PublishItem item, Document file)[] conflicts)
        {
            // no conflicts
            if (conflicts.Length == 1)
            {
                return conflicts.First();
            }

            _errorLog.Write(Errors.UrlPath.OutputPathConflict(conflicts.First().item.Path!, conflicts.Select(x => x.file.FilePath)));

            var publishItem = conflicts.First().item;

            // redirection file is preferred than source file
            // otherwise, prefer the one based on FilePath
            var redirectionFiles = conflicts.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                return (publishItem, redirectionFiles.OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First());
            }

            return (publishItem, conflicts.Select(x => x.file).OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First());
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
            _errorLog.Write(Errors.UrlPath.PublishUrlConflict(publishItem.Url, conflictingFiles, conflictMonikers));

            var itemsWithoutMoniker = conflicts.Where(x => x.item.MonikerGroup is null);
            if (itemsWithoutMoniker.Count() == 1)
            {
                return itemsWithoutMoniker.First();
            }
            else if (itemsWithoutMoniker.Count() > 1)
            {
                return itemsWithoutMoniker.OrderByDescending(x => x.file.FilePath.Path, PathUtility.PathComparer).First();
            }

            var latestMonikerGroup = conflicts.OrderByDescending(x => x.item.MonikerGroup, PathUtility.PathComparer).First().item.MonikerGroup;
            var itemsWithChosenMonikerGroup = conflicts.Where(x => x.item.MonikerGroup == latestMonikerGroup);
            if (itemsWithChosenMonikerGroup.Count() == 1)
            {
                return itemsWithChosenMonikerGroup.First();
            }

            var redirectionFiles = itemsWithChosenMonikerGroup.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                return (publishItem, redirectionFiles.OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First());
            }

            return (publishItem, itemsWithChosenMonikerGroup.Select(x => x.file).OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First());
        }

        private void AddToPublishMapping(ListBuilder<(PublishItem item, Document file)> outputMapping, FilePath path)
        {
            var file = _documentProvider.GetDocument(path);
            var (monikerErrors, monikers) = _monikerProvider.GetFileLevelMonikers(path);
            _errorLog.Write(monikerErrors);
            var outputPath = "";
            switch (file.ContentType)
            {
                case ContentType.Redirection:
                    outputPath = _config.Legacy ? _documentProvider.GetOutputPath(path) : null;
                    break;
                case ContentType.Page:
                case ContentType.TableOfContents:
                    outputPath = _documentProvider.GetOutputPath(path);
                    break;
                case ContentType.Resource:
                    outputPath = _documentProvider.GetOutputPath(path);

                    if (!_config.CopyResources &&
                        _input.TryGetPhysicalPath(path, out var physicalPath))
                    {
                        outputPath = PathUtility.NormalizeFile(Path.GetRelativePath(_outputPath, physicalPath));
                    }
                    break;
            }

            var publishItem = new PublishItem(
                file.SiteUrl,
                outputPath,
                _sourceMap.GetOriginalFilePath(path) ?? path.Path,
                _locale,
                monikers,
                _monikerProvider.GetConfigMonikerRange(path),
                file.ContentType,
                file.Mime);
            outputMapping.Add((publishItem, file));
        }
    }
}
