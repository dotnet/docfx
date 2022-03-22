// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Docs.Validation;

namespace Microsoft.Docs.Build;

internal class TocLoader
{
    private readonly BuildOptions _buildOptions;
    private readonly Input _input;
    private readonly LinkResolver _linkResolver;
    private readonly XrefResolver _xrefResolver;
    private readonly TocParser _parser;
    private readonly MonikerProvider _monikerProvider;
    private readonly DependencyMapBuilder _dependencyMapBuilder;
    private readonly ContentValidator _contentValidator;
    private readonly ErrorBuilder _errors;
    private readonly IReadOnlyDictionary<string, JoinTOCConfig> _joinTOCConfigs;
    private readonly BuildScope _buildScope;

    private readonly MemoryCache<FilePath, Watch<(TocNode, List<FilePath>, List<FilePath>, List<FilePath>)>> _cache = new();

    private static readonly string[] s_tocFileNames = new[] { "toc.md", "toc.json", "toc.yml" };

    private static readonly AsyncLocal<ImmutableStack<FilePath>> s_recursionDetector = new();

    public TocLoader(
        BuildOptions buildOptions,
        Input input,
        LinkResolver linkResolver,
        XrefResolver xrefResolver,
        TocParser parser,
        MonikerProvider monikerProvider,
        DependencyMapBuilder dependencyMapBuilder,
        ContentValidator contentValidator,
        Config config,
        ErrorBuilder errors,
        BuildScope buildScope)
    {
        _buildOptions = buildOptions;
        _input = input;
        _linkResolver = linkResolver;
        _xrefResolver = xrefResolver;
        _parser = parser;
        _monikerProvider = monikerProvider;
        _dependencyMapBuilder = dependencyMapBuilder;
        _contentValidator = contentValidator;
        _errors = errors;
        _buildScope = buildScope;
        _joinTOCConfigs = config.JoinTOC.Where(x => x.ReferenceToc != null).ToDictionary(x => PathUtility.Normalize(x.ReferenceToc!));
    }

    public static TocHrefType GetHrefType(string? href)
    {
        var linkType = UrlUtility.GetLinkType(href);
        if (linkType == LinkType.AbsolutePath || linkType == LinkType.External)
        {
            return TocHrefType.AbsolutePath;
        }

        var (path, _, _) = UrlUtility.SplitUrl(href ?? "");
        if (path.EndsWith('/') || path.EndsWith('\\'))
        {
            return TocHrefType.RelativeFolder;
        }

        var fileName = Path.GetFileName(path);

        if (s_tocFileNames.Any(s => s.Equals(fileName, PathUtility.PathComparison)))
        {
            return TocHrefType.TocFile;
        }

        return TocHrefType.RelativeFile;
    }

    public (TocNode node, List<FilePath> referencedFiles, List<FilePath> referencedTocs, List<FilePath> servicePages) Load(FilePath file)
    {
        return _cache.GetOrAdd(file, _ => new(() =>
        {
            var referencedFiles = new List<FilePath>();
            var referencedTocs = new List<FilePath>();
            var (node, servicePages) = LoadTocFile(file, file, referencedFiles, referencedTocs);

            return (node, referencedFiles, referencedTocs, servicePages);
        })).Value;
    }

    private TocNode JoinToc(TocNode referenceToc, string topLevelTocFilePath, JoinTOCConfig joinTOCConfig)
    {
        var topLevelToc = _parser.Parse(FilePath.Content(new PathString(topLevelTocFilePath)), _errors);
        TraverseAndConvertHref(topLevelToc, joinTOCConfig);
        TraverseAndMerge(topLevelToc, referenceToc.Items, new HashSet<TocNode>());
        return topLevelToc;
    }

    private void TraverseAndConvertHref(TocNode node, JoinTOCConfig joinTOCConfig)
    {
        if (!string.IsNullOrEmpty(node.Href.Value) && !(node.Href.Value.StartsWith("~/") || node.Href.Value.StartsWith("~\\")))
        {
            // convert href in TopLevelTOC to one that relative to ReferenceTOC
            var referenceTOCRelativeDir = Path.GetDirectoryName(joinTOCConfig.ReferenceToc) ?? ".";
            var referenceTOCFullPath = Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, referenceTOCRelativeDir));

            var topLevelTOCRelativeDir = Path.GetDirectoryName(joinTOCConfig.TopLevelToc) ?? ".";
            var topLevelTOCFullPath = Path.GetFullPath(Path.Combine(_buildOptions.DocsetPath, topLevelTOCRelativeDir));

            var hrefFullPath = Path.GetFullPath(Path.Combine(topLevelTOCFullPath, node.Href.Value));

            var hrefRelativeToReferenceTOC = Path.GetRelativePath(referenceTOCFullPath, hrefFullPath);

            node.Href = node.Href.With(hrefRelativeToReferenceTOC);
        }
        foreach (var item in node.Items)
        {
            TraverseAndConvertHref(item, joinTOCConfig);
        }
    }

    private void TraverseAndMerge(
        TocNode node,
        List<SourceInfo<TocNode>> itemsToMatch,
        HashSet<TocNode> matched)
    {
        foreach (var pattern in node.Children)
        {
            foreach (var item in itemsToMatch)
            {
                if (item.Value.Name != null && !matched.Contains(item) && GlobUtility.CreateGlobMatcher(pattern)(item.Value.Name!))
                {
                    matched.Add(item);
                    node.Items.Add(item);
                }
            }
        }

        foreach (var item in node.Items)
        {
            TraverseAndMerge(item, itemsToMatch, matched);
        }
    }

    private (TocNode node, List<FilePath> servicePages) LoadTocFile(
        FilePath file, FilePath rootPath, List<FilePath> referencedFiles, List<FilePath> referencedTocs)
    {
        var servicePages = new List<FilePath>();

        // add to parent path
        s_recursionDetector.Value ??= ImmutableStack<FilePath>.Empty;

        var recursionDetector = s_recursionDetector.Value!;
        if (recursionDetector.Contains(file))
        {
            throw Errors.Link.CircularReference(new SourceInfo(file, 1, 1), file, recursionDetector).ToException();
        }

        try
        {
            recursionDetector = recursionDetector.Push(file);
            s_recursionDetector.Value = recursionDetector;

            var node = _parser.Parse(file, _errors);

            // Generate service pages.
            if (_joinTOCConfigs.TryGetValue(file.Path, out var joinTOCConfig) && joinTOCConfig != null)
            {
                if (joinTOCConfig.TopLevelToc != null)
                {
                    var topLevelTocFullPath = Path.Combine(_buildOptions.DocsetPath, joinTOCConfig.TopLevelToc);

                    if (File.Exists(topLevelTocFullPath))
                    {
                        node = JoinToc(node, joinTOCConfig.TopLevelToc, joinTOCConfig);

                        // Generate Service Page.
                        var servicePage = new ServicePageGenerator(_buildOptions.DocsetPath, _input, joinTOCConfig, _buildScope, _parser, _errors);

                        foreach (var item in node.Items)
                        {
                            servicePage.GenerateServicePageFromTopLevelTOC(item, servicePages);
                        }
                        AddOverviewPage(node);
                    }
                }
            }

            // Resolve
            node.Items = LoadTocNodes(node.Items, file, rootPath, referencedFiles, referencedTocs);

            if (file == rootPath)
            {
                _contentValidator.ValidateTocEntryDuplicated(file, referencedFiles);
            }
            return (node, servicePages);
        }
        finally
        {
            s_recursionDetector.Value = recursionDetector.Pop();
        }
    }

    private void AddOverviewPage(TocNode toc)
    {
        if (toc.Items.Count == 0)
        {
            return;
        }

        foreach (var child in toc.Items)
        {
            AddOverviewPage(child);
        }

        if (!string.IsNullOrEmpty(toc.Uid) || !string.IsNullOrEmpty(toc.Href))
        {
            var overview = new TocNode(toc);
            overview.Expanded = false;
            overview.Name = overview.Name.With("Overview");
            toc.Items.Insert(0, new SourceInfo<TocNode>(overview));
            toc.Href = toc.Href.With(null);
            toc.Uid = toc.Uid.With(null);
        }
    }

    private List<SourceInfo<TocNode>> LoadTocNodes(
        List<SourceInfo<TocNode>> nodes,
        FilePath filePath,
        FilePath rootPath,
        List<FilePath> referencedFiles,
        List<FilePath> referencedTocs)
    {
        var newNodes = new SourceInfo<TocNode>[nodes.Count];

        Parallel.For(0, nodes.Count, i =>
        {
            var newReferencedFiles = new List<FilePath>();
            var newReferencedTocs = new List<FilePath>();
            newNodes[i] = LoadTocNode(nodes[i], filePath, rootPath, newReferencedFiles, newReferencedTocs);
            lock (newNodes)
            {
                referencedFiles.AddRange(newReferencedFiles);
                referencedTocs.AddRange(newReferencedTocs);
            }
        });

        return newNodes.ToList();
    }

    private SourceInfo<TocNode> LoadTocNode(
        SourceInfo<TocNode> node,
        FilePath filePath,
        FilePath rootPath,
        List<FilePath> referencedFiles,
        List<FilePath> referencedTocs)
    {
        // process
        var tocHref = GetTocHref(node);
        var topicHref = GetTopicHref(node);
        var topicUid = node.Value.Uid;

        _contentValidator.ValidateTocBreadcrumbLinkExternal(filePath, node);

        var (resolvedTocHref, subChildren, subChildrenFirstItem, tocHrefType) = ProcessTocHref(
            filePath, rootPath, referencedFiles, referencedTocs, tocHref);
        var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(
            filePath, rootPath, referencedFiles, topicUid, topicHref, addToReferencedFiles: !IsTocIncludeHref(tocHrefType));

        // resolve children
        var items = subChildren?.Items ?? LoadTocNodes(node.Value.Items, filePath, rootPath, referencedFiles, referencedTocs);

        // set resolved href/document back
        var newNode = new TocNode(node)
        {
            Href = resolvedTocHref.Or(resolvedTopicHref).Or(subChildrenFirstItem?.Href),
            TocHref = default,
            TopicHref = default,
            Homepage = string.IsNullOrEmpty(node.Value.Href) && !string.IsNullOrEmpty(node.Value.TopicHref)
                ? resolvedTopicHref : default,
            Name = node.Value.Name.Or(resolvedTopicName),
            Document = document ?? subChildrenFirstItem?.Document,
            Items = items,
        };

        // resolve monikers
        newNode.Monikers = GetMonikers(newNode, filePath, rootPath);

        // validate
        if (string.IsNullOrEmpty(newNode.Name))
        {
            _errors.Add(Errors.JsonSchema.MissingAttribute(newNode.Name.Source ?? node.Source, "name"));
        }

        return new SourceInfo<TocNode>(newNode, node);
    }

    private MonikerList GetMonikers(TocNode currentItem, FilePath filePath, FilePath rootPath)
    {
        var monikers = MonikerList.Union(GetMonikerLists(currentItem));

        if (filePath.Path.Value.Contains("_splitted/"))
        {
            monikers = new MonikerList(monikers.Union(_monikerProvider.GetFileLevelMonikers(_errors, rootPath)));
        }

        foreach (var item in currentItem.Items)
        {
            if (monikers == item.Value.Monikers)
            {
                item.Value.Monikers = default;
            }
        }
        return monikers;
    }

    private IEnumerable<MonikerList> GetMonikerLists(TocNode currentItem)
    {
        if (!string.IsNullOrEmpty(currentItem.Href))
        {
            var linkType = UrlUtility.GetLinkType(currentItem.Href);
            if (linkType == LinkType.External || linkType == LinkType.AbsolutePath)
            {
                yield return default;
            }
            else if (currentItem.Document != null)
            {
                yield return _monikerProvider.GetFileLevelMonikers(_errors, currentItem.Document);
            }
        }

        // Union with children's monikers
        foreach (var item in currentItem.Items)
        {
            yield return item.Value.Monikers;
        }
    }

    private SourceInfo<string?> GetTocHref(TocNode tocInputModel)
    {
        if (!string.IsNullOrEmpty(tocInputModel.TocHref))
        {
            var tocHrefType = GetHrefType(tocInputModel.TocHref);
            if (IsTocIncludeHref(tocHrefType) || tocHrefType == TocHrefType.AbsolutePath)
            {
                return tocInputModel.TocHref;
            }
            else
            {
                _errors.AddIfNotNull(Errors.Toc.InvalidTocHref(tocInputModel.TocHref));
            }
        }

        if (!string.IsNullOrEmpty(tocInputModel.Href) && IsTocIncludeHref(GetHrefType(tocInputModel.Href)))
        {
            return tocInputModel.Href;
        }

        return default;
    }

    private SourceInfo<string?> GetTopicHref(TocNode tocInputModel)
    {
        if (!string.IsNullOrEmpty(tocInputModel.TopicHref))
        {
            var topicHrefType = GetHrefType(tocInputModel.TopicHref);
            if (IsTocIncludeHref(topicHrefType))
            {
                _errors.Add(Errors.Toc.InvalidTopicHref(tocInputModel.TopicHref));
            }
            else
            {
                return tocInputModel.TopicHref;
            }
        }

        if (string.IsNullOrEmpty(tocInputModel.Href) || !IsTocIncludeHref(GetHrefType(tocInputModel.Href)))
        {
            return tocInputModel.Href;
        }

        return default;
    }

    private (SourceInfo<string?> resolvedTocHref, TocNode? subChildren, TocNode? subChildrenFirstItem, TocHrefType tocHrefType)
        ProcessTocHref(
            FilePath filePath,
            FilePath rootPath,
            List<FilePath> referencedFiles,
            List<FilePath> referencedTocs,
            SourceInfo<string?> tocHref)
    {
        if (string.IsNullOrEmpty(tocHref))
        {
            return (tocHref, default, default, default);
        }

        var tocHrefType = GetHrefType(tocHref);
        Debug.Assert(tocHrefType == TocHrefType.AbsolutePath || IsTocIncludeHref(tocHrefType));

        if (tocHrefType == TocHrefType.AbsolutePath)
        {
            return (tocHref, default, default, default);
        }

        var (hrefPath, _, _) = UrlUtility.SplitUrl(tocHref.Value ?? "");
        var referenceTocFilePath = ResolveTocHref(filePath, referencedTocs, tocHrefType, new SourceInfo<string>(hrefPath, tocHref));
        if (referenceTocFilePath != null)
        {
            var (nestedToc, _) = LoadTocFile(
                referenceTocFilePath,
                rootPath,
                tocHrefType == TocHrefType.RelativeFolder ? new List<FilePath>() : referencedFiles,
                referencedTocs);

            if (tocHrefType == TocHrefType.RelativeFolder)
            {
                var nestedTocFirstItem = GetFirstItem(nestedToc.Items);
                _dependencyMapBuilder.AddDependencyItem(filePath, nestedTocFirstItem?.Document, DependencyType.File);
                return (default, default, nestedTocFirstItem, tocHrefType);
            }

            return (default, nestedToc, default, tocHrefType);
        }

        return default;
    }

    private (SourceInfo<string?> resolvedTopicHref, SourceInfo<string?> resolvedTopicName, FilePath? file) ProcessTopicItem(
        FilePath filePath,
        FilePath rootPath,
        List<FilePath> referencedFiles,
        SourceInfo<string?> uid,
        SourceInfo<string?> topicHref,
        bool addToReferencedFiles = true)
    {
        // process href first
        if (!string.IsNullOrEmpty(topicHref))
        {
            var topicHrefType = GetHrefType(topicHref);
            Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsTocIncludeHref(topicHrefType));

            var (linkErrors, link, resolvedFile) = _linkResolver.ResolveLink(topicHref!, filePath, rootPath, new HyperLinkNode
            {
                HyperLinkType = HyperLinkType.Default,
                IsVisible = true,  // workaround to skip 'link-text-missing' validation
                UrlLink = topicHref!.Value!,
                SourceInfo = topicHref!.Source!,
            });
            _errors.AddRange(linkErrors);

            if (resolvedFile != null && addToReferencedFiles)
            {
                // add to referenced document list
                referencedFiles.Add(resolvedFile);
            }
            return (new SourceInfo<string?>(link, topicHref), default, resolvedFile);
        }

        // process uid then if href is empty or null
        if (!string.IsNullOrEmpty(uid.Value))
        {
            var (uidError, xrefLink) = _xrefResolver.ResolveXrefByUid(
                uid!, filePath, rootPath, _monikerProvider.GetFileLevelMonikers(ErrorBuilder.Null, filePath));
            _errors.AddIfNotNull(uidError);

            if (xrefLink.DeclaringFile != null && addToReferencedFiles)
            {
                referencedFiles.Add(xrefLink.DeclaringFile);
            }

            if (!string.IsNullOrEmpty(xrefLink.Href))
            {
                return (new SourceInfo<string?>(xrefLink.Href, uid), new SourceInfo<string?>(xrefLink.Display, uid), xrefLink.DeclaringFile);
            }
        }

        // if both uid and href are empty or null, return default
        return (topicHref, default, default);
    }

    private FilePath? ResolveTocHref(
        FilePath filePath, List<FilePath> referencedTocs, TocHrefType tocHrefType, SourceInfo<string> href)
    {
        switch (tocHrefType)
        {
            case TocHrefType.RelativeFolder:
                var result = default(FilePath);
                foreach (var name in s_tocFileNames)
                {
                    var probingHref = new SourceInfo<string>(Path.Combine(href, name), href);
                    var (_, subToc) = _linkResolver.ResolveContent(probingHref, filePath, filePath, transitive: false);
                    if (subToc != null)
                    {
                        if (!subToc.IsGitCommit)
                        {
                            return subToc;
                        }
                        else if (result is null)
                        {
                            result = subToc;
                        }
                    }
                }
                if (result == null)
                {
                    _errors.Add(Errors.Toc.TocNotFound(href));
                }
                return result;

            case TocHrefType.TocFile:

                // NOTE: to keep v2 parity, TOC include does not transit.
                var (error, referencedToc) = _linkResolver.ResolveContent(href, filePath, filePath, transitive: false);
                _errors.AddIfNotNull(error);
                referencedTocs.AddIfNotNull(referencedToc);
                return referencedToc;

            default:
                return default;
        }
    }

    private static TocNode? GetFirstItem(List<SourceInfo<TocNode>> items)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Value.Href))
            {
                return item;
            }
        }

        foreach (var item in items)
        {
            return GetFirstItem(item.Value.Items);
        }

        return null;
    }

    private static bool IsTocIncludeHref(TocHrefType tocHrefType)
    {
        return tocHrefType == TocHrefType.TocFile || tocHrefType == TocHrefType.RelativeFolder;
    }
}
