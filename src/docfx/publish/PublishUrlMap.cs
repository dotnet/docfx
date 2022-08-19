// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Microsoft.Docs.Build;

internal class PublishUrlMap
{
    private readonly Config _config;
    private readonly ErrorBuilder _errors;
    private readonly BuildScope _buildScope;
    private readonly RedirectionProvider _redirectionProvider;
    private readonly DocumentProvider _documentProvider;
    private readonly MonikerProvider _monikerProvider;

    private readonly Watch<(FilePath[] files, Dictionary<string, List<PublishUrlMapItem>> map)> _state;
    private readonly ConcurrentDictionary<FilePath, Watch<string?>> _canonicalVersionCache = new();

    public PublishUrlMap(
        Config config,
        ErrorBuilder errors,
        BuildScope buildScope,
        RedirectionProvider redirectionProvider,
        DocumentProvider documentProvider,
        MonikerProvider monikerProvider)
    {
        _config = config;
        _errors = errors;
        _buildScope = buildScope;
        _redirectionProvider = redirectionProvider;
        _documentProvider = documentProvider;
        _monikerProvider = monikerProvider;
        _state = new(Initialize);
    }

    public string? GetCanonicalVersion(FilePath file)
    {
        // If the file does not have versioning configured, assume it does not have canonical version,
        // this avoids the expensive creation of url map.
        var monikers = _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, file);
        if (!monikers.HasMonikers)
        {
            return default;
        }

        return _canonicalVersionCache.GetOrAdd(file, key => new(() => GetCanonicalVersionCore(key))).Value;
    }

    public IEnumerable<FilePath> GetFilesByUrl(string url)
    {
        if (_state.Value.map.TryGetValue(url, out var items))
        {
            return items.Select(x => x.SourcePath);
        }
        return Array.Empty<FilePath>();
    }

    public IEnumerable<FilePath> GetFiles() => _state.Value.files;

    public FilePath[] ResolveUrlConflicts(LogScope scope, IEnumerable<FilePath> files)
    {
        return CreateUrlMap(scope, files).files;
    }

    private string? GetCanonicalVersionCore(FilePath file)
    {
        var url = _documentProvider.GetSiteUrl(file);
        if (_state.Value.map.TryGetValue(url, out var item))
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

    private (FilePath[] files, Dictionary<string, List<PublishUrlMapItem>> urlMap) Initialize()
    {
        using var scope = Progress.Start("Building publish url map");

        return CreateUrlMap(
            scope,
            _redirectionProvider.Files.Concat(
            _buildScope.GetFiles(ContentType.Resource).Where(x => x.Origin != FileOrigin.Fallback || _config.OutputType == OutputType.Html)).Concat(
            _buildScope.GetFiles(ContentType.Page).Where(x => x.Origin != FileOrigin.Fallback)));
    }

    private (FilePath[] files, Dictionary<string, List<PublishUrlMapItem>> urlMap) CreateUrlMap(LogScope scope, IEnumerable<FilePath> files)
    {
        var builder = new ListBuilder<PublishUrlMapItem>();

        ParallelUtility.ForEach(scope, _errors, files, file =>
        {
            var siteUrl = _documentProvider.GetSiteUrl(file);
            var outputPath = _documentProvider.GetOutputPath(file);
            var monikers = _monikerProvider.GetFileLevelMonikers(_errors, file);
            builder.Add(new PublishUrlMapItem(siteUrl, outputPath, monikers, file));
        });

        // resolve output path conflicts
        var publishMapWithoutOutputPathConflicts =
            builder.AsList().GroupBy(x => x.OutputPath, PathUtility.PathComparer).Select(g => ResolveOutputPathConflicts(g));

        // resolve publish url conflicts
        var urlMap = publishMapWithoutOutputPathConflicts
               .GroupBy(x => x)
               .Select(g => ResolvePublishUrlConflicts(g))
               .GroupBy(x => x.Url)
               .ToDictionary(g => g.Key, g => g.ToList());

        return (urlMap.Values.SelectMany(item => item).Select(item => item.SourcePath).ToArray(), urlMap);
    }

    private PublishUrlMapItem ResolveOutputPathConflicts(IGrouping<string, PublishUrlMapItem> conflicts)
    {
        if (conflicts.Count() == 1)
        {
            return conflicts.First();
        }

        // redirection file is preferred than source file
        var redirections = conflicts.Where(x => x.SourcePath.Origin == FileOrigin.Redirection).OrderBy(x => x.SourcePath.Path, PathUtility.PathComparer);
        var nonRedirections = conflicts.Where(x => x.SourcePath.Origin != FileOrigin.Redirection)
            .OrderBy(x => x.SourcePath.Path, PathUtility.PathComparer)
            .Select(x => x.SourcePath.Path.Value);
        var redirection = redirections.FirstOrDefault();
        var redirectionCount = redirections.Count();

        if (redirectionCount == 0)
        {
            _errors.Add(Errors.UrlPath.OutputPathConflict(conflicts.First().OutputPath, conflicts.Select(x => x.SourcePath)));
            return conflicts.OrderBy(x => x.SourcePath.Path, PathUtility.PathComparer).Last();
        }
        else if (redirectionCount == 1)
        {
            _errors.Add(Errors.Redirection.RedirectedFileNotRemoved(nonRedirections));
            return redirection!;
        }
        else
        {
            if (conflicts.Count() > redirectionCount)
            {
                _errors.Add(Errors.Redirection.RedirectedFileNotRemoved(nonRedirections));
            }

            _errors.Add(Errors.UrlPath.OutputPathConflict(conflicts.First().OutputPath, conflicts.Select(x => x.SourcePath)));
            return redirection!;
        }
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
        _errors.Add(Errors.UrlPath.PublishUrlConflict(conflicts.First().Url, null, conflictingFiles, conflictMonikers));

        return conflicts.OrderBy(x => x).Last();
    }
}
