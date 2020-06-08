// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMapBuilder
    {
        private readonly Config _config;
        private readonly ErrorLog _errorLog;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly TableOfContentsMap _tocMap;
        private readonly SourceMap _sourceMap;

        private Dictionary<string, List<(PublishUrlMapItem publishItem, Document file)>> _publishUrlMap = new Dictionary<string, List<(PublishUrlMapItem, Document)>>();

        public PublishUrlMapBuilder(
            Config config,
            ErrorLog errorLog,
            BuildScope buildScope,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            TableOfContentsMap tocMap,
            SourceMap sourceMap)
        {
            _config = config;
            _errorLog = errorLog;
            _buildScope = buildScope;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _tocMap = tocMap;
            _sourceMap = sourceMap;

            Initialize();
        }

        public IEnumerable<FilePath> GetBuildFiles()
        {
            return _publishUrlMap.Values.SelectMany(x => x).Select(x => x.file.FilePath);
        }

        public IEnumerable<(PublishUrlMapItem, Document)> GetPublishOutput()
        {
            return _publishUrlMap.Values.SelectMany(x => x);
        }

        private void Initialize()
        {
            var builder = new ListBuilder<(PublishUrlMapItem item, Document file)>();
            var files = _redirectionProvider.Files.Where(x => x.Origin != FileOrigin.Fallback)
                         .Concat(_buildScope.GetFiles(ContentType.Resource).Where(x => x.Origin != FileOrigin.Fallback || _config.OutputType == OutputType.Html))
                         .Concat(_buildScope.GetFiles(ContentType.Page).Where(x => x.Origin != FileOrigin.Fallback))
                         .Concat(_tocMap.GetFiles());
            using (Progress.Start("Building publish map"))
            {
                ParallelUtility.ForEach(
                    _errorLog,
                    files,
                    file => AddItem(builder, file));
            }

            // resolve output path conflicts
            var publishMap = builder.ToList();
            var groupByOutputPath = publishMap.Where(x => x.item.OutputPath != null).GroupBy(x => x.item.OutputPath, PathUtility.PathComparer);
            var temp = publishMap.Where(x => x.item.OutputPath is null)
                .Concat(groupByOutputPath.Where(g => g.Count() == 1).SelectMany(g => g))
                .Concat(groupByOutputPath.Where(g => g.Count() > 1).Select(g => ResolveOutputPathConflicts(g.ToArray())));

            // resolve publish url conflicts
            var groupByPublishUrl = temp.GroupBy(x => x.item, new PublishUrlMapItemComparer());
            var test = groupByPublishUrl.Where(g => g.Count() == 1).SelectMany(g => g)
                            .Concat(groupByPublishUrl.Where(g => g.Count() > 1).Select(g => ResolvePublishUrlConflicts(g.ToArray())));

            _publishUrlMap = test.GroupBy(x => x.Item1.Url).ToDictionary(g => g.Key, g => g.ToList());
        }

        private (PublishUrlMapItem item, Document file) ResolveOutputPathConflicts((PublishUrlMapItem item, Document file)[] conflicts)
        {
            // no conflicts
            if (conflicts.Length == 1)
            {
                return conflicts.First();
            }

            _errorLog.Write(Errors.UrlPath.OutputPathConflict(conflicts.First().item.OutputPath, conflicts.Select(x => x.file.FilePath)));
            var item = conflicts.First().item;

            // redirection file is preferred than source file
            // otherwise, prefer the one based on FilePath
            var redirectionFiles = conflicts.Where(x => x.file.ContentType == ContentType.Redirection).Select(x => x.file);
            if (redirectionFiles.Any())
            {
                var chosen = redirectionFiles.OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First();
                return (item, chosen);
            }

            return (item, conflicts.Select(x => x.file).OrderByDescending(x => x.FilePath.Path, PathUtility.PathComparer).First());
        }

        private (PublishUrlMapItem, Document) ResolvePublishUrlConflicts((PublishUrlMapItem item, Document file)[] conflicts)
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

            var itemsWithoutMoniker = conflicts.Where(x => x.item.Monikers.MonikerGroup is null);
            if (itemsWithoutMoniker.Count() == 1)
            {
                return itemsWithoutMoniker.First();
            }
            else if (itemsWithoutMoniker.Count() > 1)
            {
                return itemsWithoutMoniker.OrderByDescending(x => x.file.FilePath.Path, PathUtility.PathComparer).First();
            }

            var latestMonikerGroup = conflicts.OrderByDescending(x => x.item.Monikers.MonikerGroup, PathUtility.PathComparer).First().item.Monikers.MonikerGroup;
            var itemsWithChosenMonikerGroup = conflicts.Where(x => x.item.Monikers.MonikerGroup == latestMonikerGroup);
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

        private void AddItem(ListBuilder<(PublishUrlMapItem item, Document file)> outputMapping, FilePath path)
        {
            var file = _documentProvider.GetDocument(path);
            var (monikerErrors, monikers) = _monikerProvider.GetFileLevelMonikers(path);
            _errorLog.Write(monikerErrors);
            var outputPath = _documentProvider.GetOutputPath(path);
            outputMapping.Add((new PublishUrlMapItem(file.SiteUrl, outputPath, monikers, _sourceMap.GetOriginalFilePath(path) ?? path.Path), file));
        }
    }
}
