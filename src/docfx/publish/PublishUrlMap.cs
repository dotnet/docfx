// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMap
    {
        private readonly Config _config;
        private readonly ErrorBuilder _errors;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly TableOfContentsMap _tocMap;

        private readonly HashSet<FilePath> _files;
        private readonly IReadOnlyDictionary<FilePath, PublishUrlMapItem> _sourcePathMap;
        private readonly IReadOnlyDictionary<string, List<PublishUrlMapItem>> _publishUrlMap;
        private readonly ConcurrentDictionary<string, string?> _canonicalVersionMap = new ConcurrentDictionary<string, string?>();

        public PublishUrlMap(
            Config config,
            ErrorBuilder errors,
            BuildScope buildScope,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            TableOfContentsMap tocMap)
        {
            _config = config;
            _errors = errors;
            _buildScope = buildScope;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _tocMap = tocMap;
            _publishUrlMap = Initialize();
            _sourcePathMap = _publishUrlMap.Values.SelectMany(x => x).ToDictionary(x => x.SourcePath);
            _files = _publishUrlMap.Values.SelectMany(x => x).Select(x => x.SourcePath).ToHashSet();
        }

        public string? GetCanonicalVersion(FilePath file)
        {
            var url = _documentProvider.GetSiteUrl(file);
            return _canonicalVersionMap.GetOrAdd(url, GetCanonicalVersionCore);
        }

        public IEnumerable<FilePath> GetFilesByUrl(string url)
        {
            if (_publishUrlMap.TryGetValue(url, out var items))
            {
                return items.Select(x => x.SourcePath);
            }
            return Array.Empty<FilePath>();
        }

        public string TryGetPublishUrl(FilePath source)
        {
            return _sourcePathMap[source].Url;
        }

        public HashSet<FilePath> GetAllFiles() => _files;

        public IEnumerable<(string url, FilePath sourcePath, MonikerList monikers)> GetPublishOutput()
        {
            return _publishUrlMap.Values.SelectMany(x => x).Select(x => (x.Url, x.SourcePath, x.Monikers));
        }

        private string? GetCanonicalVersionCore(string url)
        {
            if (_publishUrlMap.TryGetValue(url, out var item))
            {
                string? canonicalVersion = null;
                var order = 0;
                foreach (var moniker in item.SelectMany(x => x.Monikers))
                {
                    var currentOrder = _monikerProvider.GetMonikerOrder(moniker);
                    if (currentOrder > order)
                    {
                        canonicalVersion = moniker;
                        order = currentOrder;
                    }
                }
                return canonicalVersion;
            }
            return default;
        }

        private Dictionary<string, List<PublishUrlMapItem>> Initialize()
        {
            var builder = new ListBuilder<PublishUrlMapItem>();

            using (Progress.Start("Building publish url map"))
            {
                Parallel.Invoke(
                    () => ParallelUtility.ForEach(
                        _errors, _redirectionProvider.Files.Where(x => x.Origin != FileOrigin.Fallback), file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(
                        _errors,
                        _buildScope.GetFiles(ContentType.Resource).Where(x => x.Origin != FileOrigin.Fallback || _config.OutputType == OutputType.Html),
                        file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(
                        _errors, _buildScope.GetFiles(ContentType.Page).Where(x => x.Origin != FileOrigin.Fallback), file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(_errors, _tocMap.GetFiles(), file => AddItem(builder, file)));
            }

            // resolve output path conflicts
            var publishMapWithoutOutputPathConflicts =
                builder.AsList().GroupBy(x => x.OutputPath, PathUtility.PathComparer).Select(g => ResolveOutputPathConflicts(g));

            // resolve publish url conflicts
            return publishMapWithoutOutputPathConflicts
                   .GroupBy(x => x)
                   .Select(g => ResolvePublishUrlConflicts(g))
                   .GroupBy(x => x.Url)
                   .ToDictionary(g => g.Key, g => g.ToList());
        }

        private PublishUrlMapItem ResolveOutputPathConflicts(IGrouping<string, PublishUrlMapItem> conflicts)
        {
            if (conflicts.Count() == 1)
            {
                return conflicts.First();
            }

            _errors.Add(Errors.UrlPath.OutputPathConflict(conflicts.First().OutputPath, conflicts.Select(x => x.SourcePath)));

            // redirection file is preferred than source file
            // otherwise, prefer the one based on FilePath
            return conflicts.OrderBy(x => x.SourcePath.Origin.ToString(), PathUtility.PathComparer)
                .ThenBy(x => x.SourcePath.Path, PathUtility.PathComparer).Last();
        }

        private PublishUrlMapItem ResolvePublishUrlConflicts(IGrouping<PublishUrlMapItem, PublishUrlMapItem> conflicts)
        {
            if (conflicts.Count() == 1)
            {
                return conflicts.First();
            }

            var conflictMonikers = conflicts
                .SelectMany(x => x.Monikers)
                .GroupBy(moniker => moniker)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var conflictingFiles = conflicts.ToDictionary(x => x.SourcePath, x => x.Monikers);
            _errors.Add(Errors.UrlPath.PublishUrlConflict(conflicts.First().Url, conflictingFiles, conflictMonikers));

            return conflicts.OrderBy(x => x).Last();
        }

        private void AddItem(ListBuilder<PublishUrlMapItem> outputMapping, FilePath path)
        {
            var siteUrl = _documentProvider.GetSiteUrl(path);
            var outputPath = _documentProvider.GetOutputPath(path);
            var monikers = _monikerProvider.GetFileLevelMonikers(_errors, path);
            outputMapping.Add(new PublishUrlMapItem(siteUrl, outputPath, monikers, path));
        }
    }
}
