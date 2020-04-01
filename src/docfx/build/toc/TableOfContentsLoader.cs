// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsLoader
    {
        private readonly Input _input;
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly TableOfContentsParser _parser;
        private readonly MonikerProvider _monikerProvider;
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        private readonly ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsNode, List<Document>, List<Document>)> _cache =
                     new ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsNode, List<Document>, List<Document>)>();

        private static readonly string[] s_tocFileNames = new[] { "TOC.md", "TOC.json", "TOC.yml" };
        private static readonly string[] s_experimentalTocFileNames = new[] { "TOC.experimental.md", "TOC.experimental.json", "TOC.experimental.yml" };

        private static ThreadLocal<Stack<Document>> t_recursionDetector = new ThreadLocal<Stack<Document>>(() => new Stack<Document>());

        public TableOfContentsLoader(
            Input input,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            TableOfContentsParser parser,
            MonikerProvider monikerProvider,
            DependencyMapBuilder dependencyMapBuilder)
        {
            _input = input;
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
                var content = _input.ReadString(file.FilePath);
                var node = LoadTocFile(content, file, file, referencedFiles, referencedTocs, errors);

                return (errors, node, referencedFiles, referencedTocs);
            });
        }

        private TableOfContentsNode LoadTocFile(
            string content,
            Document file,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            List<Error> errors)
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

                var node = _parser.Parse(content, file.FilePath, errors);
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
                var tocHref = GetTocHref(node, errors);
                var topicHref = GetTopicHref(node, errors);
                var topicUid = node.Value.Uid;

                var (resolvedTocHref, subChildren, subChildrenFirstItem) = ProcessTocHref(
                    filePath, rootPath, referencedFiles, referencedTocs, tocHref, errors);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(
                    filePath, rootPath, referencedFiles, topicUid, topicHref, errors);

                // set resolved href/document back
                var newItem = new TableOfContentsNode(node)
                {
                    Href = resolvedTocHref.Or(resolvedTopicHref).Or(subChildrenFirstItem?.Href),
                    TocHref = resolvedTocHref,
                    Homepage = string.IsNullOrEmpty(node.Value.Href) && !string.IsNullOrEmpty(node.Value.TopicHref)
                        ? resolvedTopicHref : default,
                    Name = node.Value.Name.Or(resolvedTopicName),
                    Document = document ?? subChildrenFirstItem?.Document,
                    Items = subChildren?.Items ?? node.Value.Items,
                };

                // resolve children
                if (subChildren is null)
                {
                    newItem.Items = LoadTocNode(node.Value.Items, filePath, rootPath, referencedFiles, referencedTocs, errors);
                }

                // resolve monikers
                newItem.Monikers = GetMonikers(newItem, errors);
                newItems.Add(new SourceInfo<TableOfContentsNode>(newItem, node.Source));

                // validate
                // todo: how to do required validation in strong model
                if (string.IsNullOrEmpty(newItem.Name))
                {
                    errors.Add(Errors.TableOfContents.MissingTocHead(newItem.Name.Source ?? node.Source));
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
                else
                {
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

        private SourceInfo<string?> GetTocHref(TableOfContentsNode tocInputModel, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(tocInputModel.TocHref))
            {
                var tocHrefType = GetHrefType(tocInputModel.TocHref);
                if (IsIncludeHref(tocHrefType) || tocHrefType == TocHrefType.AbsolutePath)
                {
                    return tocInputModel.TocHref;
                }
                else
                {
                    errors.AddIfNotNull(Errors.TableOfContents.InvalidTocHref(tocInputModel.TocHref));
                }
            }

            if (!string.IsNullOrEmpty(tocInputModel.Href) && IsIncludeHref(GetHrefType(tocInputModel.Href)))
            {
                return tocInputModel.Href;
            }

            return default;
        }

        private SourceInfo<string?> GetTopicHref(TableOfContentsNode tocInputModel, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(tocInputModel.TopicHref))
            {
                var topicHrefType = GetHrefType(tocInputModel.TopicHref);
                if (IsIncludeHref(topicHrefType))
                {
                    errors.Add(Errors.TableOfContents.InvalidTopicHref(tocInputModel.TopicHref));
                }
                else
                {
                    return tocInputModel.TopicHref;
                }
            }

            if (string.IsNullOrEmpty(tocInputModel.Href) || !IsIncludeHref(GetHrefType(tocInputModel.Href)))
            {
                return tocInputModel.Href;
            }

            return default;
        }

        private (SourceInfo<string?> resolvedTocHref, TableOfContentsNode? subChildren, TableOfContentsNode? subChildrenFirstItem)
            ProcessTocHref(
                Document filePath,
                Document rootPath,
                List<Document> referencedFiles,
                List<Document> referencedTocs,
                SourceInfo<string?> tocHref,
                List<Error> errors)
        {
            if (string.IsNullOrEmpty(tocHref))
            {
                return (tocHref, default, default);
            }

            var tocHrefType = GetHrefType(tocHref);
            Debug.Assert(tocHrefType == TocHrefType.AbsolutePath || IsIncludeHref(tocHrefType));

            if (tocHrefType == TocHrefType.AbsolutePath)
            {
                return (tocHref, default, default);
            }

            var (hrefPath, _, _) = UrlUtility.SplitUrl(tocHref.Value ?? "");

            var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(
                filePath, referencedTocs, tocHrefType, new SourceInfo<string>(hrefPath, tocHref), errors);
            if (referencedTocContent != null && referenceTocFilePath != null)
            {
                var nestedToc = LoadTocFile(
                    referencedTocContent,
                    referenceTocFilePath,
                    rootPath,
                    tocHrefType == TocHrefType.RelativeFolder ? new List<Document>() : referencedFiles,
                    referencedTocs,
                    errors);

                if (tocHrefType == TocHrefType.RelativeFolder)
                {
                    var nestedTocFirstItem = GetFirstItem(nestedToc.Items);
                    _dependencyMapBuilder.AddDependencyItem(filePath, nestedTocFirstItem?.Document, DependencyType.Link);
                    return (default, default, nestedTocFirstItem);
                }

                return (default, nestedToc, default);
            }

            return default;
        }

        private (SourceInfo<string?> resolvedTopicHref, SourceInfo<string?> resolvedTopicName, Document? file) ProcessTopicItem(
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            SourceInfo<string?> uid,
            SourceInfo<string?> topicHref,
            List<Error> errors)
        {
            // process uid first
            if (!string.IsNullOrEmpty(uid.Value))
            {
                var (uidError, uidLink, display, declaringFile) = _xrefResolver.ResolveXref(new SourceInfo<string>(uid.Value, uid), filePath, rootPath);
                errors.AddIfNotNull(uidError);

                if (declaringFile != null)
                {
                    referencedFiles.Add(declaringFile);
                }

                if (!string.IsNullOrEmpty(uidLink))
                {
                    return (new SourceInfo<string?>(uidLink, uid), new SourceInfo<string?>(display, uid), declaringFile);
                }
            }

            // process topicHref then
            if (string.IsNullOrEmpty(topicHref))
            {
                return (topicHref, default, default);
            }

            var topicHrefType = GetHrefType(topicHref);
            Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

            var (error, link, resolvedFile) = _linkResolver.ResolveLink(topicHref!, filePath, rootPath);
            errors.AddIfNotNull(error);

            if (resolvedFile != null)
            {
                // add to referenced document list
                referencedFiles.Add(resolvedFile);
            }
            return (new SourceInfo<string?>(link, topicHref), default, resolvedFile);
        }

        private (string? content, Document? filePath) ResolveTocHrefContent(
            Document filePath,
            List<Document> referencedTocs,
            TocHrefType tocHrefType,
            SourceInfo<string> href,
            List<Error> errors)
        {
            switch (tocHrefType)
            {
                case TocHrefType.RelativeFolder:
                    (string, Document) result = default;
                    foreach (var tocFileName in s_tocFileNames)
                    {
                        var subToc = Resolve(tocFileName);
                        if (subToc != null)
                        {
                            if (!subToc.Value.filePath.FilePath.IsGitCommit)
                            {
                                return subToc.Value;
                            }
                            else if (result == default)
                            {
                                result = subToc.Value;
                            }
                        }
                    }
                    return result;

                case TocHrefType.TocFile:
                    var (error, referencedTocContent, referencedToc) = _linkResolver.ResolveContent(
                        href, filePath, DependencyType.TocInclusion);
                    errors.AddIfNotNull(error);

                    if (referencedToc != null)
                    {
                        // add to referenced toc list
                        referencedTocs.Add(referencedToc);
                    }

                    return (referencedTocContent, referencedToc);

                default:
                    return default;
            }

            (string content, Document filePath)? Resolve(string name)
            {
                var (_, referencedTocContent, referencedToc) = _linkResolver.ResolveContent(
                    new SourceInfo<string>(Path.Combine(href, name), href), filePath, DependencyType.TocInclusion);

                if (referencedTocContent != null && referencedToc != null)
                    return (referencedTocContent, referencedToc);

                return null;
            }
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

        private static bool IsIncludeHref(TocHrefType tocHrefType)
        {
            return tocHrefType == TocHrefType.TocFile || tocHrefType == TocHrefType.RelativeFolder;
        }

        private static TocHrefType GetHrefType(string? href)
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

            if (s_tocFileNames.Concat(s_experimentalTocFileNames).Any(s => s.Equals(fileName, PathUtility.PathComparison)))
            {
                return TocHrefType.TocFile;
            }

            return TocHrefType.RelativeFile;
        }

        private enum TocHrefType
        {
            None,
            AbsolutePath,
            RelativeFile,
            RelativeFolder,
            TocFile,
        }
    }
}
