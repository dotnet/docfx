// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TableOfContentsParser
    {
        private static readonly string[] s_tocFileNames = new[] { "TOC.md", "TOC.json", "TOC.yml" };
        private static readonly string[] s_experimentalTocFileNames = new[] { "TOC.experimental.md", "TOC.experimental.json", "TOC.experimental.yml" };
        private static ThreadLocal<Stack<Document>> t_recursionDetector = new ThreadLocal<Stack<Document>>(() => new Stack<Document>());

        public static (List<Error> errors, TableOfContentsModel model, List<Document> referencedFiles, List<Document> referencedTocs)
            Load(Context context, Document file)
        {
            var referencedFiles = new List<Document>();
            var referencedTocs = new List<Document>();

            var (errors, model) = LoadInternal(context, file, file, referencedFiles, referencedTocs);

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(error);

            model.Metadata.Monikers = monikers;
            return (errors, model, referencedFiles, referencedTocs);
        }

        private static (List<Error> errors, TableOfContentsModel tocModel) LoadTocModel(Context context, Document file, string content = null)
        {
            var filePath = file.FilePath;

            if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content is null ? YamlUtility.Parse(file, context) : YamlUtility.Parse(content, file.FilePath);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content is null ? JsonUtility.Parse(file, context) : JsonUtility.Parse(content, file.FilePath);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".md", PathUtility.PathComparison))
            {
                content = content ?? context.Input.ReadString(file.FilePath);
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                return MarkdownTocMarkup.Parse(context.MarkdownEngine, content, file);
            }

            throw new NotSupportedException($"{filePath} is an unknown TOC file");
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

        private static (List<Error> errors, TableOfContentsModel model) LoadInternal(
            Context context,
            Document file,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            string content = null)
        {
            // add to parent path
            if (t_recursionDetector.Value.Contains(file))
            {
                var dependencyChain = t_recursionDetector.Value.Reverse().ToList();
                dependencyChain.Add(file);
                throw Errors.CircularReference(dependencyChain, file).ToException();
            }

            var (errors, model) = LoadTocModel(context, file, content);

            if (model.Items.Count > 0)
            {
                try
                {
                    t_recursionDetector.Value.Push(file);
                    var (resolveErros, newItems) = ResolveTocModelItems(context, model.Items, file, rootPath, referencedFiles, referencedTocs);
                    errors.AddRange(resolveErros);
                    model.Items = newItems;
                }
                finally
                {
                    Debug.Assert(t_recursionDetector.Value.Count > 0);
                    t_recursionDetector.Value.Pop();
                }
            }

            return (errors, model);
        }

        private static (List<Error> errors, List<TableOfContentsItem> items) ResolveTocModelItems(
            Context context,
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
                    context, filePath, rootPath, referencedFiles, referencedTocs, tocHref, errors);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(
                    context, filePath, rootPath, referencedFiles, topicUid, topicHref, errors);

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
                    Monikers = string.IsNullOrEmpty(resolvedTocHref) && string.IsNullOrEmpty(resolvedTopicHref)
                        ? subChildrenFirstItem?.Monikers ?? new List<string>() : new List<string>(),
                };

                // resolve children
                if (subChildren == null && tocModelItem.Items != null)
                {
                    var (subErrors, subItems) = ResolveTocModelItems(
                        context, tocModelItem.Items, filePath, rootPath, referencedFiles, referencedTocs);
                    newItem.Items = subItems;
                    errors.AddRange(subErrors);
                }

                // resolve monikers
                newItem.Monikers = GetMonikers(context, rootPath, newItem, errors);
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

        private static List<string> GetMonikers(Context context, Document rootPath, TableOfContentsItem currentItem, List<Error> errors)
        {
            var monikers = new List<string>();
            if (currentItem.Monikers.Any())
            {
                monikers = currentItem.Monikers;
            }
            else if (!string.IsNullOrEmpty(currentItem.Href))
            {
                var linkType = UrlUtility.GetLinkType(currentItem.Href);
                if (linkType == LinkType.External || linkType == LinkType.AbsolutePath)
                {
                    var (error, rootFileMonikers) = context.MonikerProvider.GetFileLevelMonikers(rootPath);
                    errors.AddIfNotNull(error);

                    monikers = rootFileMonikers;
                }
                else
                {
                    if (currentItem.Document != null)
                    {
                        var (error, referenceFileMonikers) = context.MonikerProvider.GetFileLevelMonikers(currentItem.Document);
                        errors.AddIfNotNull(error);

                        monikers = referenceFileMonikers;
                    }
                }
            }

            // Union with children's monikers
            var childrenMonikers = currentItem.Items?.SelectMany(c => c.Monikers) ?? new List<string>();
            monikers = childrenMonikers.Union(monikers).Distinct().ToList();
            monikers.Sort(context.MonikerProvider.Comparer);
            return monikers;
        }

        private static SourceInfo<string> GetTocHref(TableOfContentsItem tocInputModel, List<Error> errors)
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

        private static SourceInfo<string> GetTopicHref(TableOfContentsItem tocInputModel, List<Error> errors)
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

        private static (SourceInfo<string> resolvedTocHref, TableOfContentsModel subChildren, TableOfContentsItem subChildrenFirstItem)
            ProcessTocHref(
            Context context,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            SourceInfo<string> tocHref,
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

            var (hrefPath, _, _) = UrlUtility.SplitUrl(tocHref);

            var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(
                context, filePath, referencedTocs, tocHrefType, new SourceInfo<string>(hrefPath, tocHref), errors);
            if (referencedTocContent != null)
            {
                var (subErrors, nestedToc) = LoadInternal(
                    context, referenceTocFilePath, rootPath, referencedFiles, referencedTocs, referencedTocContent);
                errors.AddRange(subErrors);

                if (tocHrefType == TocHrefType.RelativeFolder)
                {
                    var nestedTocFirstItem = GetFirstItem(nestedToc.Items);
                    context.DependencyMapBuilder.AddDependencyItem(filePath, nestedTocFirstItem?.Document, DependencyType.Link);
                    return (default, default, nestedTocFirstItem);
                }

                return (default, nestedToc, default);
            }

            return default;
        }

        private static (SourceInfo<string> resolvedTopicHref, SourceInfo<string> resolvedTopicName, Document file) ProcessTopicItem(
            Context context,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            SourceInfo<string> uid,
            SourceInfo<string> topicHref,
            List<Error> errors)
        {
            // process uid first
            if (!string.IsNullOrEmpty(uid))
            {
                var (uidError, uidLink, display, declaringFile) = context.XrefResolver.ResolveRelativeXref(rootPath, uid, filePath);
                errors.AddIfNotNull(uidError);

                if (declaringFile != null)
                {
                    referencedFiles.Add(declaringFile);
                }

                if (!string.IsNullOrEmpty(uidLink))
                {
                    return (new SourceInfo<string>(uidLink, uid), new SourceInfo<string>(display, uid), declaringFile);
                }
            }

            // process topicHref then
            if (string.IsNullOrEmpty(topicHref))
            {
                return (topicHref, default, default);
            }

            var topicHrefType = GetHrefType(topicHref);
            Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

            var (error, link, resolvedFile) = context.DependencyResolver.ResolveRelativeLink(rootPath, topicHref, filePath);
            errors.AddIfNotNull(error);

            if (resolvedFile != null)
            {
                // add to referenced document list
                referencedFiles.Add(resolvedFile);
            }
            return (new SourceInfo<string>(link, topicHref), default, resolvedFile);
        }

        private static (string content, Document filePath) ResolveTocHrefContent(
            Context context,
            Document filePath,
            List<Document> referencedTocs,
            TocHrefType tocHrefType,
            SourceInfo<string> href,
            List<Error> errors)
        {
            switch (tocHrefType)
            {
                case TocHrefType.RelativeFolder:
                    foreach (var tocFileName in s_tocFileNames)
                    {
                        var subToc = Resolve(tocFileName);
                        if (subToc != null)
                        {
                            return subToc.Value;
                        }
                    }
                    return default;

                case TocHrefType.TocFile:
                    var (error, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(
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
                var (_, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(
                    new SourceInfo<string>(Path.Combine(href, name), href), filePath, DependencyType.TocInclusion);

                if (referencedTocContent != null && referencedToc != null)
                    return (referencedTocContent, referencedToc);

                return null;
            }
        }

        private static TableOfContentsItem GetFirstItem(List<TableOfContentsItem> items)
        {
            if (items == null)
                return null;

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

        private static TocHrefType GetHrefType(string href)
        {
            var linkType = UrlUtility.GetLinkType(href);
            if (linkType == LinkType.AbsolutePath || linkType == LinkType.External)
            {
                return TocHrefType.AbsolutePath;
            }

            var (path, _, _) = UrlUtility.SplitUrl(href);
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
