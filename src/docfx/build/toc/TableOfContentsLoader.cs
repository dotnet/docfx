// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsLoader
    {
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly TableOfContentsParser _parser;
        private readonly MonikerProvider _monikerProvider;
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        private readonly ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsNode, List<Document>, List<Document>)> _cache =
                     new ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsNode, List<Document>, List<Document>)>();

        private static readonly HashSet<string> s_tocFileNames = new HashSet<string>(PathUtility.PathComparer)
        {
            "TOC.md", "TOC.json", "TOC.yml",
            "TOC.experimental.md", "TOC.experimental.json", "TOC.experimental.yml",
        };

        private static ThreadLocal<Stack<Document>> t_recursionDetector = new ThreadLocal<Stack<Document>>(() => new Stack<Document>());

        public TableOfContentsLoader(
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            TableOfContentsParser parser,
            MonikerProvider monikerProvider,
            DependencyMapBuilder dependencyMapBuilder)
        {
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _parser = parser;
            _monikerProvider = monikerProvider;
            _dependencyMapBuilder = dependencyMapBuilder;
        }

        public (List<Error> errors, TableOfContentsNode node, List<Document> referencedFiles, List<Document> referencedTocs)
            Load(Document file)
        {
            return _cache.GetOrAdd(file.FilePath, _ =>
            {
                var referencedFiles = new List<Document>();
                var referencedTocs = new List<Document>();
                var errors = new List<Error>();
                var node = LoadTocFile(file, file, referencedFiles, referencedTocs, errors);

                return (errors, node, referencedFiles, referencedTocs);
            });
        }

        private TableOfContentsNode LoadTocFile(
            Document file, Document rootPath, List<Document> referencedFiles, List<Document> referencedTocs, List<Error> errors)
        {
            // add to parent path
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(file))
            {
                throw Errors.Link.CircularReference(new SourceInfo(file.FilePath, 1, 1), file, recursionDetector).ToException();
            }

            try
            {
                recursionDetector.Push(file);

                var node = _parser.Parse(file.FilePath, errors);
                node.Items = LoadTocNode(node.Items, file, rootPath, referencedFiles, referencedTocs, errors);
                return node;
            }
            finally
            {
                recursionDetector.Pop();
            }
        }

        private List<SourceInfo<TableOfContentsNode>> LoadTocNode(
            List<SourceInfo<TableOfContentsNode>> nodes,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            List<Error> errors)
        {
            var newItems = new List<SourceInfo<TableOfContentsNode>>();
            foreach (var node in nodes)
            {
                // process
                var (subChildren, subChildrenFirstItem) = ResolveToc(
                    filePath, rootPath, node, referencedFiles, referencedTocs, errors);
                var (resolvedTopicHref, resolvedTopicName, document) = ResolveTopic(
                    filePath, rootPath, node, referencedFiles, errors);

                // resolve children
                var items = subChildren?.Items ?? LoadTocNode(node.Value.Items, filePath, rootPath, referencedFiles, referencedTocs, errors);

                // set resolved href/document back
                var newItem = new TableOfContentsNode(node)
                {
                    Href = resolvedTopicHref.Or(subChildrenFirstItem?.Href),
                    TocHref = default,
                    TopicHref = default,
                    Homepage = string.IsNullOrEmpty(node.Value.Href) && !string.IsNullOrEmpty(node.Value.TopicHref)
                        ? resolvedTopicHref : default,
                    Name = node.Value.Name.Or(resolvedTopicName),
                    Document = document ?? subChildrenFirstItem?.Document,
                    Items = items,
                };

                // resolve monikers
                newItem.Monikers = GetMonikers(newItem, errors);
                newItems.Add(new SourceInfo<TableOfContentsNode>(newItem, node.Source));

                // validate
                if (string.IsNullOrEmpty(newItem.Name))
                {
                    errors.Add(Errors.TableOfContents.MissingTocName(newItem.Name.Source ?? node.Source));
                }
            }

            return newItems;
        }

        private IReadOnlyList<string> GetMonikers(TableOfContentsNode currentItem, List<Error> errors)
        {
            var monikers = new List<string>();
            if (!string.IsNullOrEmpty(currentItem.Href))
            {
                var linkType = UrlUtility.GetLinkType(currentItem.Href);
                if (linkType == LinkType.External || linkType == LinkType.AbsolutePath)
                {
                    return Array.Empty<string>();
                }

                if (currentItem.Document != null)
                {
                    var (monikerErrors, referenceFileMonikers) = _monikerProvider.GetFileLevelMonikers(currentItem.Document.FilePath);
                    errors.AddRange(monikerErrors);

                    if (referenceFileMonikers.Length == 0)
                    {
                        return Array.Empty<string>();
                    }
                    monikers = referenceFileMonikers.ToList();
                }
            }

            // Union with children's monikers
            if (currentItem.Items?.Count > 0)
            {
                foreach (var item in currentItem.Items)
                {
                    if (item.Value.Monikers.Count == 0)
                    {
                        return Array.Empty<string>();
                    }
                    monikers = monikers.Union(item.Value.Monikers).Distinct().ToList();
                }
            }
            monikers.Sort(StringComparer.OrdinalIgnoreCase);
            return monikers.ToArray();
        }

        private (SourceInfo<string?> resolvedTopicHref, SourceInfo<string?> resolvedTopicName, Document? file) ResolveTopic(
            Document filePath, Document rootPath, TableOfContentsNode node, List<Document> referencedFiles, List<Error> errors)
        {
            // Process uid
            if (!string.IsNullOrEmpty(node.Uid.Value))
            {
                var (uidError, uidLink, display, declaringFile) = _xrefResolver.ResolveXref(node.Uid!, filePath, rootPath);
                errors.AddIfNotNull(uidError);

                if (declaringFile != null)
                {
                    referencedFiles.Add(declaringFile);
                }

                if (!string.IsNullOrEmpty(uidLink))
                {
                    return (new SourceInfo<string?>(uidLink, node.Uid), new SourceInfo<string?>(display, node.Uid), declaringFile);
                }
            }

            // Process topicHref or href
            var href = node.TopicHref.Or(node.Href);
            if (string.IsNullOrEmpty(href))
            {
                return default;
            }

            switch (GetTocLinkType(href))
            {
                case TableOfContentsLinkType.Folder:
                case TableOfContentsLinkType.TocFile:
                    if (!string.IsNullOrEmpty(node.TopicHref))
                    {
                        errors.Add(Errors.TableOfContents.InvalidTopicHref(node.TopicHref));
                    }
                    return default;
            }

            var (error, link, resolvedFile) = _linkResolver.ResolveLink(href!, filePath, rootPath);
            errors.AddIfNotNull(error);

            if (resolvedFile != null)
            {
                // Add to referenced document list
                referencedFiles.Add(resolvedFile);
            }
            return (new SourceInfo<string?>(link, href), default, resolvedFile);
        }

        private (TableOfContentsNode? items, TableOfContentsNode? firstDecadant)
            ResolveToc(Document filePath, Document rootPath, TableOfContentsNode node, List<Document> referencedFiles, List<Document> referencedTocs, List<Error> errors)
        {
            var href = node.TocHref.Or(node.Href);

            switch (GetTocLinkType(href))
            {
                case TableOfContentsLinkType.TocFile:
                    var (error, nestedTocFile) = _linkResolver.ResolveContent(href!, filePath, DependencyType.TocInclusion);
                    errors.AddIfNotNull(error);
                    if (nestedTocFile is null)
                    {
                        return default;
                    }

                    referencedTocs.Add(nestedTocFile);
                    var nestedToc = LoadTocFile(nestedTocFile, rootPath, referencedFiles, referencedTocs, errors);
                    return (nestedToc, default);

                case TableOfContentsLinkType.Folder:
                    var linkedTocFile = FindTocInFolder(href!, filePath);
                    if (linkedTocFile is null)
                    {
                        return default;
                    }

                    var linkedToc = LoadTocFile(linkedTocFile, rootPath, new List<Document>(), referencedTocs, errors);
                    var firstDecadant = GetFirstItem(linkedToc.Items);
                    _dependencyMapBuilder.AddDependencyItem(filePath, firstDecadant?.Document, DependencyType.Link);
                    return (default, firstDecadant);

                case TableOfContentsLinkType.AbsolutePath:
                    return default;

                default:
                    if (!string.IsNullOrEmpty(node.TocHref))
                    {
                        errors.Add(Errors.TableOfContents.InvalidTocHref(node.TocHref));
                    }
                    return default;
            }
        }

        private Document? FindTocInFolder(SourceInfo<string> href, Document filePath)
        {
            var result = default(Document);
            var (hrefPath, _, _) = UrlUtility.SplitUrl(href);
            foreach (var name in s_tocFileNames)
            {
                var tocHref = new SourceInfo<string>(Path.Combine(hrefPath, name), href);
                var (_, subToc) = _linkResolver.ResolveContent(tocHref, filePath, DependencyType.TocInclusion);
                if (subToc != null)
                {
                    if (!subToc.FilePath.IsGitCommit)
                    {
                        return subToc;
                    }
                    else if (result is null)
                    {
                        result = subToc;
                    }
                }
            }
            return result;
        }

        private static TableOfContentsNode? GetFirstItem(List<SourceInfo<TableOfContentsNode>> items)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Value.Href))
                    return item;
            }

            foreach (var item in items)
            {
                return GetFirstItem(item.Value.Items);
            }

            return null;
        }

        private static TableOfContentsLinkType GetTocLinkType(string? href)
        {
            if (string.IsNullOrEmpty(href))
            {
                return TableOfContentsLinkType.Other;
            }

            switch (UrlUtility.GetLinkType(href))
            {
                case LinkType.AbsolutePath:
                    return TableOfContentsLinkType.AbsolutePath;

                case LinkType.RelativePath:
                    var (path, _, _) = UrlUtility.SplitUrl(href);
                    if (path.EndsWith('/') || path.EndsWith('\\'))
                    {
                        return TableOfContentsLinkType.Folder;
                    }

                    if (s_tocFileNames.Contains(Path.GetFileName(path)))
                    {
                        return TableOfContentsLinkType.TocFile;
                    }
                    return TableOfContentsLinkType.Other;

                default:
                    return TableOfContentsLinkType.Other;
            }
        }
    }
}
