// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TableOfContentsParser
    {
        private static readonly string[] s_tocFileNames = new[] { "TOC.md", "TOC.json", "TOC.yml" };
        private static readonly string[] s_experimentalTocFileNames = new[] { "TOC.experimental.md", "TOC.experimental.json", "TOC.experimental.yml" };

        public static (List<Error> errors, TableOfContentsModel model, List<Document> referencedFiles, List<Document> referencedTocs)
            Load(Context context, Document file)
        {
            var referencedFiles = new List<Document>();
            var referencedTocs = new List<Document>();

            var (errors, model, _) = LoadInternal(context, file, file, referencedFiles, referencedTocs, new List<Document>());

            var (error, monikers) = context.MonikerProvider.GetFileLevelMonikers(file);
            errors.AddIfNotNull(error);

            model.Metadata.Monikers = monikers;
            return (errors, model, referencedFiles, referencedTocs);
        }

        private static (List<Error> errors, TableOfContentsModel tocModel) LoadTocModel(Context context, Document file, string content = null)
        {
            var filePath = file.FilePath;

            if (file.IsFromHistory)
            {
                Debug.Assert(!string.IsNullOrEmpty(content));
            }

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
                content = content ?? file.ReadText();
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                return MarkdownTocMarkup.Parse(content, file);
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

        private static (List<Error> errors, TableOfContentsModel model, (TableOfContentsItem item, Document doc) firstChild) LoadInternal(
            Context context,
            Document file,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs,
            List<Document> parents,
            string content = null)
        {
            // add to parent path
            if (parents.Contains(file))
            {
                parents.Add(file);
                throw Errors.CircularReference(parents).ToException();
            }

            var (errors, model) = LoadTocModel(context, file, content);

            (TableOfContentsItem item, Document doc) firstChild = default;
            if (model.Items.Count > 0)
            {
                parents.Add(file);
                var (resolveErros, newItems, resolvedFirstChild) = ResolveTocModelItems(context, model.Items, parents, file, rootPath, referencedFiles, referencedTocs);
                errors.AddRange(resolveErros);
                firstChild = resolvedFirstChild;
                model.Items = newItems;
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, model, firstChild);
        }

        private static (List<Error> errors, List<TableOfContentsItem> items, (TableOfContentsItem item, Document doc) firstChild) ResolveTocModelItems(
            Context context,
            List<TableOfContentsItem> tocModelItems,
            List<Document> parents,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs)
        {
            var errors = new List<Error>();
            var subFirstChildren = new List<(TableOfContentsItem item, Document doc)>();

            (TableOfContentsItem item, Document doc) firstChild = default;
            var newTocModelItems = new List<TableOfContentsItem>();
            foreach (var tocModelItem in tocModelItems)
            {
                // process
                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, subChildren, (firstItem, firstDoc), tocHrefType) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(topicUid, topicHref);

                // set resolved href back
                var newTocModelItem = new TableOfContentsItem();
                newTocModelItem.Href = resolvedTocHref.Or(resolvedTopicHref).Or((tocHrefType == TocHrefType.RelativeFolder ? firstItem : null)?.Href);
                newTocModelItem.TocHref = resolvedTocHref;
                newTocModelItem.Homepage = !string.IsNullOrEmpty(tocModelItem.TopicHref) ? resolvedTopicHref : default;
                newTocModelItem.Name = tocModelItem.Name.Or(resolvedTopicName);
                newTocModelItem.Items = (tocHrefType == TocHrefType.TocFile ? subChildren : null)?.Items ?? tocModelItem.Items;

                var childrenMonikers = firstItem?.Monikers;
                if (subChildren == null && tocModelItem.Items != null)
                {
                    var (subErrors, subItems, subFirstChild) = ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, referencedFiles, referencedTocs);
                    (firstItem, firstDoc) = subFirstChild;
                    childrenMonikers = subItems.SelectMany(i => i.Monikers).ToList();
                    newTocModelItem.Items = subItems;
                    errors.AddRange(subErrors);
                }

                tocModelItem.Monikers = GetMonikers(resolvedTocHref, resolvedTopicHref, childrenMonikers, document);
                newTocModelItems.Add(newTocModelItem);

                if (firstChild == default)
                {
                    if (!string.IsNullOrEmpty(tocModelItem.Href))
                    {
                        firstChild.item = tocModelItem;
                        firstChild.doc = document ?? firstDoc;
                    }
                    else
                    {
                        firstChild.item = firstItem;
                        firstChild.doc = firstDoc;
                    }
                }

                // validate
                // todo: how to do required validation in strong model
                if (string.IsNullOrEmpty(tocModelItem.Name))
                {
                    errors.Add(Errors.MissingTocHead(tocModelItem.Name));
                }
            }

            return (errors, newTocModelItems, firstChild);

            List<string> GetMonikers(
                string resolvedTocHref,
                string resolvedTopicHref,
                List<string> subMonikers,
                Document document)
            {
                var monikers = new List<string>();
                if (!string.IsNullOrEmpty(resolvedTocHref) || !string.IsNullOrEmpty(resolvedTopicHref))
                {
                    var linkType = UrlUtility.GetLinkType(resolvedTopicHref);
                    if (linkType == LinkType.External || linkType == LinkType.AbsolutePath)
                    {
                        var (error, rootFileMonikers) = context.MonikerProvider.GetFileLevelMonikers(rootPath);
                        errors.AddIfNotNull(error);

                        monikers = rootFileMonikers;
                    }
                    else
                    {
                        if (document != null)
                        {
                            var (error, referenceFileMonikers) = context.MonikerProvider.GetFileLevelMonikers(document);
                            errors.AddIfNotNull(error);

                            monikers = referenceFileMonikers;
                        }
                        else
                        {
                            monikers = new List<string>();
                        }
                    }
                }

                // Union with children's monikers
                var childrenMonikers = subMonikers ?? new List<string>();
                monikers = childrenMonikers.Union(monikers).Distinct().ToList();
                monikers.Sort(context.MonikerProvider.Comparer);
                return monikers;
            }

            SourceInfo<string> GetTocHref(TableOfContentsItem tocInputModel)
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

            SourceInfo<string> GetTopicHref(TableOfContentsItem tocInputModel)
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

            (SourceInfo<string> resolvedTocHref, TableOfContentsModel subChildren, (TableOfContentsItem item, Document doc) firstChild, TocHrefType tocHrefType) ProcessTocHref(SourceInfo<string> tocHref)
            {
                if (string.IsNullOrEmpty(tocHref))
                {
                    return (tocHref, default, default, default);
                }

                var tocHrefType = GetHrefType(tocHref);
                Debug.Assert(tocHrefType == TocHrefType.AbsolutePath || IsIncludeHref(tocHrefType));

                if (tocHrefType == TocHrefType.AbsolutePath)
                {
                    return (tocHref, default, default, tocHrefType);
                }

                var (hrefPath, fragment, query) = UrlUtility.SplitUrl(tocHref);

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, new SourceInfo<string>(hrefPath, tocHref));
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedToc, (firstItem, firstItemDoc)) = LoadInternal(context, referenceTocFilePath, rootPath, referencedFiles, referencedTocs, parents, referencedTocContent);
                    errors.AddRange(subErrors);

                    if (tocHrefType == TocHrefType.RelativeFolder)
                    {
                        context.DependencyMapBuilder.AddDependencyItem(filePath, firstItemDoc, DependencyType.Link);
                    }

                    return (default, nestedToc, (firstItem, firstItemDoc), tocHrefType);
                }

                return default;
            }

            (SourceInfo<string> resolvedTopicHref, SourceInfo<string> resolvedTopicName, Document file) ProcessTopicItem(SourceInfo<string> uid, SourceInfo<string> topicHref)
            {
                // process uid first
                if (!string.IsNullOrEmpty(uid))
                {
                    var (uidError, uidLink, display, xrefSpec) = context.DependencyResolver.ResolveRelativeXref(rootPath, uid, filePath);
                    errors.AddIfNotNull(uidError);

                    if (xrefSpec?.DeclaringFile != null)
                    {
                        referencedFiles.Add(xrefSpec?.DeclaringFile);
                    }

                    if (!string.IsNullOrEmpty(uidLink))
                    {
                        return (new SourceInfo<string>(uidLink, uid), new SourceInfo<string>(display, uid), xrefSpec?.DeclaringFile);
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

            (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, SourceInfo<string> href)
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

                        var (error, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(href, filePath, DependencyType.TocInclusion);
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
                    var (_, referencedTocContent, referencedToc) = context.DependencyResolver.ResolveContent(new SourceInfo<string>(Path.Combine(href, name), href), filePath, DependencyType.TocInclusion);

                    if (referencedTocContent != null && referencedToc != null)
                        return (referencedTocContent, referencedToc);

                    return null;
                }
            }
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
