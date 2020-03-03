// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class TableOfContentsLoader
    {
        private readonly Input _input;
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly MarkdownEngine _markdownEngine;
        private readonly MonikerProvider _monikerProvider;
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        private readonly ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsModel, List<Document>, List<Document>)> _cache =
                     new ConcurrentDictionary<FilePath, (List<Error>, TableOfContentsModel, List<Document>, List<Document>)>();

        private static readonly string[] s_tocFileNames = new[] { "TOC.md", "TOC.json", "TOC.yml" };
        private static readonly string[] s_experimentalTocFileNames = new[] { "TOC.experimental.md", "TOC.experimental.json", "TOC.experimental.yml" };

        private static ThreadLocal<Stack<Document>> t_recursionDetector = new ThreadLocal<Stack<Document>>(() => new Stack<Document>());

        public TableOfContentsLoader(
            Input input,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            MarkdownEngine markdownEngine,
            MonikerProvider monikerProvider,
            DependencyMapBuilder dependencyMapBuilder)
        {
            _input = input;
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _markdownEngine = markdownEngine;
            _monikerProvider = monikerProvider;
            _dependencyMapBuilder = dependencyMapBuilder;
        }

        public (List<Error> errors, TableOfContentsModel model, List<Document> referencedFiles, List<Document> referencedTocs)
            Load(Document file)
        {
            return _cache.GetOrAdd(file.FilePath, _ =>
            {
                var referencedFiles = new List<Document>();
                var referencedTocs = new List<Document>();

                var (errors, model) = LoadInternal(file, file, referencedFiles, referencedTocs);

                var (error, monikers) = _monikerProvider.GetFileLevelMonikers(file.FilePath);
                errors.AddIfNotNull(error);

                model.Metadata.Monikers = monikers;
                return (errors, model, referencedFiles, referencedTocs);
            });
        }

        private (List<Error> errors, TableOfContentsModel tocModel) LoadTocModel(FilePath file, string? content = null)
        {
            if (file.EndsWith(".yml"))
            {
                var (errors, tocToken) = content is null ? _input.ReadYaml(file) : YamlUtility.Parse(content, file);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (file.EndsWith(".json"))
            {
                var (errors, tocToken) = content is null ? _input.ReadJson(file) : JsonUtility.Parse(content, file);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (file.EndsWith(".md"))
            {
                content = content ?? _input.ReadString(file);
                return TableOfContentsMarkup.Parse(_markdownEngine, content, file);
            }

            throw new NotSupportedException($"'{file}' is an unknown TOC file");
        }

        private static (List<Error>, TableOfContentsModel) LoadTocModel(JToken tocToken)
        {
            if (tocToken is JArray tocArray)
            {
                // toc model
                var (errors, items) = JsonUtility.ToObject<List<TableOfContentsItem>>(tocArray);
                return (errors, new TableOfContentsModel
                {
                    Items = items,
                });
            }
            else if (tocToken is JObject tocObject)
            {
                // toc root model
                return JsonUtility.ToObject<TableOfContentsModel>(tocObject);
            }
            return (new List<Error>(), new TableOfContentsModel());
        }

        private (List<Error> errors, TableOfContentsModel model) LoadInternal(
            Document file,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            string? content = null)
        {
            // add to parent path
            var recursionDetector = t_recursionDetector.Value!;
            if (recursionDetector.Contains(file))
            {
                var dependencyChain = recursionDetector.Reverse().ToList();
                dependencyChain.Add(file);
                throw Errors.CircularReference(dependencyChain, file).ToException();
            }

            var (errors, model) = LoadTocModel(file.FilePath, content);

            if (model.Items.Count > 0)
            {
                try
                {
                    recursionDetector.Push(file);
                    var (resolveErros, newItems) = ResolveTocModelItems(model.Items, file, rootPath, referencedFiles, referencedTocs);
                    errors.AddRange(resolveErros);
                    model.Items = newItems;
                }
                finally
                {
                    recursionDetector.Pop();
                }
            }

            return (errors, model);
        }

        private (List<Error> errors, List<TableOfContentsItem> items) ResolveTocModelItems(
            List<TableOfContentsItem> tocModelItems,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs)
        {
            var errors = new List<Error>();
            var newItems = new List<TableOfContentsItem>();
            foreach (var tocModelItem in tocModelItems)
            {
                // process
                var tocHref = GetTocHref(tocModelItem, errors);
                var topicHref = GetTopicHref(tocModelItem, errors);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, subChildren, subChildrenFirstItem) = ProcessTocHref(
                    filePath, rootPath, referencedFiles, referencedTocs, tocHref, errors);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(
                    filePath, rootPath, referencedFiles, topicUid, topicHref, errors);

                // set resolved href/document back
                var newItem = new TableOfContentsItem(tocModelItem)
                {
                    Href = resolvedTocHref.Or(resolvedTopicHref).Or(subChildrenFirstItem?.Href),
                    TocHref = resolvedTocHref,
                    Homepage = string.IsNullOrEmpty(tocModelItem.Href) && !string.IsNullOrEmpty(tocModelItem.TopicHref)
                        ? resolvedTopicHref : default,
                    Name = tocModelItem.Name.Or(resolvedTopicName),
                    Document = document ?? subChildrenFirstItem?.Document,
                    Items = subChildren?.Items ?? tocModelItem.Items,
                };

                // resolve children
                if (subChildren is null)
                {
                    var (subErrors, subItems) = ResolveTocModelItems(
                        tocModelItem.Items, filePath, rootPath, referencedFiles, referencedTocs);
                    newItem.Items = subItems;
                    errors.AddRange(subErrors);
                }

                // resolve monikers
                newItem.Monikers = GetMonikers(newItem, errors);
                newItems.Add(newItem);

                // validate
                // todo: how to do required validation in strong model
                if (string.IsNullOrEmpty(newItem.Name))
                {
                    errors.Add(Errors.MissingTocHead(newItem.Name));
                }
            }

            return (errors, newItems);
        }

        private IReadOnlyList<string> GetMonikers(TableOfContentsItem currentItem, List<Error> errors)
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
                        var (error, referenceFileMonikers) = _monikerProvider.GetFileLevelMonikers(currentItem.Document.FilePath);
                        errors.AddIfNotNull(error);

                        if (referenceFileMonikers.Count == 0)
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
                    if (item.Monikers.Count == 0)
                    {
                        return Array.Empty<string>();
                    }
                    monikers = monikers.Union(item.Monikers).Distinct().ToList();
                }
            }
            monikers.Sort(StringComparer.OrdinalIgnoreCase);
            return monikers.ToArray();
        }

        private SourceInfo<string?> GetTocHref(TableOfContentsItem tocInputModel, List<Error> errors)
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
                    errors.AddIfNotNull(Errors.InvalidTocHref(tocInputModel.TocHref));
                }
            }

            if (!string.IsNullOrEmpty(tocInputModel.Href) && IsIncludeHref(GetHrefType(tocInputModel.Href)))
            {
                return tocInputModel.Href;
            }

            return default;
        }

        private SourceInfo<string?> GetTopicHref(TableOfContentsItem tocInputModel, List<Error> errors)
        {
            if (!string.IsNullOrEmpty(tocInputModel.TopicHref))
            {
                var topicHrefType = GetHrefType(tocInputModel.TopicHref);
                if (IsIncludeHref(topicHrefType))
                {
                    errors.Add(Errors.InvalidTopicHref(tocInputModel.TopicHref));
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

        private (SourceInfo<string?> resolvedTocHref, TableOfContentsModel? subChildren, TableOfContentsItem? subChildrenFirstItem)
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
                var (subErrors, nestedToc) = LoadInternal(
                    referenceTocFilePath,
                    rootPath,
                    tocHrefType == TocHrefType.RelativeFolder ? new List<Document>() : referencedFiles,
                    referencedTocs,
                    referencedTocContent);
                errors.AddRange(subErrors);

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
            if (!string.IsNullOrEmpty(uid))
            {
                var (uidError, uidLink, display, declaringFile) = _xrefResolver.ResolveXref(uid, filePath, rootPath);
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
                            if (!subToc.Value.filePath.FilePath.IsFromHistory)
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

        private static TableOfContentsItem? GetFirstItem(List<TableOfContentsItem> items)
        {
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Href))
                    return item;
            }

            foreach (var item in items)
            {
                return GetFirstItem(item.Items);
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
