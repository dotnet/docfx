// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class PublishUrlMap
    {
        private readonly Config _config;
        private readonly ErrorLog _errorLog;
        private readonly BuildScope _buildScope;
        private readonly RedirectionProvider _redirectionProvider;
        private readonly DocumentProvider _documentProvider;
        private readonly MonikerProvider _monikerProvider;
        private readonly TableOfContentsMap _tocMap;

        private readonly HashSet<FilePath> _files;
        private readonly IReadOnlyDictionary<string, List<PublishUrlMapItem>> _publishUrlMap;

        public PublishUrlMap(
            Config config,
            ErrorLog errorLog,
            BuildScope buildScope,
            RedirectionProvider redirectionProvider,
            DocumentProvider documentProvider,
            MonikerProvider monikerProvider,
            TableOfContentsMap tocMap)
        {
            _config = config;
            _errorLog = errorLog;
            _buildScope = buildScope;
            _redirectionProvider = redirectionProvider;
            _documentProvider = documentProvider;
            _monikerProvider = monikerProvider;
            _tocMap = tocMap;
            _publishUrlMap = Initialize();
            _files = _publishUrlMap.Values.SelectMany(x => x).Select(x => x.SourcePath).ToHashSet();
        }

        public HashSet<FilePath> GetFiles() => _files;

        public IEnumerable<(string url, FilePath sourcePath, MonikerList monikers)> GetPublishOutput()
        {
            return _publishUrlMap.Values.SelectMany(x => x).Select(x => (x.Url, x.SourcePath, x.Monikers));
        }

        private Dictionary<string, List<PublishUrlMapItem>> Initialize()
        {
            var builder = new ListBuilder<PublishUrlMapItem>();

            using (Progress.Start("Building publish url map"))
            {
                Parallel.Invoke(
                    () => ParallelUtility.ForEach(_errorLog, _redirectionProvider.Files.Where(x => x.Origin != FileOrigin.Fallback), file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(_errorLog, _buildScope.GetFiles(ContentType.Resource).Where(x => x.Origin != FileOrigin.Fallback || _config.OutputType == OutputType.Html), file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(_errorLog, _buildScope.GetFiles(ContentType.Page).Where(x => x.Origin != FileOrigin.Fallback), file => AddItem(builder, file)),
                    () => ParallelUtility.ForEach(_errorLog, _tocMap.GetFiles(), file => AddItem(builder, file)));
            }

            // resolve output path conflicts
            var publishMap = builder.ToList();
            var groupByOutputPath = publishMap.GroupBy(x => x.OutputPath, PathUtility.PathComparer).ToList();
            var publishMapWithoutOutputPathConflicts = groupByOutputPath.Where(g => g.Count() == 1).SelectMany(g => g)
                .Concat(groupByOutputPath.Where(g => g.Count() > 1).Select(g => ResolveOutputPathConflicts(g.ToArray())));

            // resolve publish url conflicts
            var groupByPublishUrl = publishMapWithoutOutputPathConflicts.GroupBy(x => x).ToList();
            return groupByPublishUrl.Where(g => g.Count() == 1).SelectMany(g => g)
                            .Concat(groupByPublishUrl.Where(g => g.Count() > 1).Select(g => ResolvePublishUrlConflicts(g.ToArray())))
                            .GroupBy(x => x.Url).ToDictionary(g => g.Key, g => g.ToList());
        }

        private PublishUrlMapItem ResolveOutputPathConflicts(PublishUrlMapItem[] conflicts)
        {
            _errorLog.Write(Errors.UrlPath.OutputPathConflict(conflicts.First().OutputPath, conflicts.Select(x => x.SourcePath)));

            // redirection file is preferred than source file
            // otherwise, prefer the one based on FilePath
            return conflicts.OrderByDescending(x => x.SourcePath.Origin.ToString(), PathUtility.PathComparer)
                .ThenByDescending(x => x.SourcePath.Path, PathUtility.PathComparer).First();
        }

        private PublishUrlMapItem ResolvePublishUrlConflicts(PublishUrlMapItem[] conflicts)
        {
            var conflictMonikers = conflicts
                .SelectMany(x => x.Monikers)
                .GroupBy(moniker => moniker)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            var conflictingFiles = conflicts.ToDictionary(x => x.SourcePath, x => x.Monikers);
            _errorLog.Write(Errors.UrlPath.PublishUrlConflict(conflicts.First().Url, conflictingFiles, conflictMonikers));

            return conflicts.OrderByDescending(x => x).First();
        }

        private void AddItem(ListBuilder<PublishUrlMapItem> outputMapping, FilePath path)
        {
            var file = _documentProvider.GetDocument(path);
            var (monikerErrors, monikers) = _monikerProvider.GetFileLevelMonikers(path);
            _errorLog.Write(monikerErrors);
            var outputPath = _documentProvider.GetOutputPath(path);
            outputMapping.Add(new PublishUrlMapItem(file.SiteUrl, outputPath, monikers, path));
        }
    }
}
