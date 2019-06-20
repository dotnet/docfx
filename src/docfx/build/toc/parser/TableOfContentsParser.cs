// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            var (errors, model) = LoadInternal(context, file, file, referencedFiles, referencedTocs, new List<Document>());

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
                return MarkdownTocMarkup.LoadMdTocModel(content, file);
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

            if (model.Items.Count > 0)
            {
                parents.Add(file);
                errors.AddRange(ResolveTocModelItems(context, model.Items, parents, file, rootPath, referencedFiles, referencedTocs));
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, model);
        }

        private static List<Error> ResolveTocModelItems(
            Context context,
            List<TableOfContentsItem> tocModelItems,
            List<Document> parents,
            Document filePath,
            Document rootPath,
            List<Document> referencedFiles,
            List<Document> referencedTocs)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, referencedFiles, referencedTocs));
                }

                // process
                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, resolvedTopicItemFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(topicUid, topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTocHref.Or(resolvedTopicHref).Or(resolvedTopicItemFromTocHref?.Href);
                tocModelItem.TocHref = resolvedTocHref;
                tocModelItem.Homepage = !string.IsNullOrEmpty(tocModelItem.TopicHref) ? resolvedTopicHref : default;
                tocModelItem.Name = tocModelItem.Name.Or(resolvedTopicName);
                tocModelItem.Items = subChildren?.Items ?? tocModelItem.Items;
                tocModelItem.Monikers = GetMonikers(resolvedTocHref, resolvedTopicHref, resolvedTopicItemFromTocHref, tocModelItem, document);

                // validate
                // todo: how to do required validation in strong model
                if (string.IsNullOrEmpty(tocModelItem.Name))
                {
                    errors.Add(Errors.MissingTocHead(tocModelItem.Name));
                }
            }

            return errors;

            List<string> GetMonikers(
                string resolvedTocHref,
                string resolvedTopicHref,
                TableOfContentsItem resolvedTopicItemFromTocHref,
                TableOfContentsItem tocModelItem,
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
                else
                {
                    monikers = resolvedTopicItemFromTocHref?.Monikers ?? new List<string>();
                }

                // Union with children's monikers
                var childrenMonikers = tocModelItem.Items?.SelectMany(child => child.Monikers) ?? new List<string>();
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

            (SourceInfo<string> resolvedTocHref, TableOfContentsItem resolvedTopicItem, TableOfContentsModel subChildren) ProcessTocHref(SourceInfo<string> tocHref)
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

                var (hrefPath, fragment, query) = UrlUtility.SplitUrl(tocHref);

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, new SourceInfo<string>(hrefPath, tocHref));
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedToc) = LoadInternal(context, referenceTocFilePath, rootPath, referencedFiles, referencedTocs, parents, referencedTocContent);
                    errors.AddRange(subErrors);
                    if (tocHrefType == TocHrefType.RelativeFolder)
                    {
                        return (default, GetFirstItemWithHref(nestedToc.Items), default);
                    }
                    else
                    {
                        return (default, default, nestedToc);
                    }
                }

                return default;
            }

            (SourceInfo<string> resolvedTopicHref, SourceInfo<string> resolvedTopicName, Document file) ProcessTopicItem(SourceInfo<string> uid, SourceInfo<string> topicHref)
            {
                // process uid first
                if (!string.IsNullOrEmpty(uid))
                {
                    var (uidError, uidLink, display, xrefSpec) = context.DependencyResolver.ResolveXref(uid, filePath, rootPath);
                    errors.AddIfNotNull(uidError);

                    if (xrefSpec?.DeclairingFile != null)
                    {
                        referencedFiles.Add(xrefSpec?.DeclairingFile);
                    }

                    if (!string.IsNullOrEmpty(uidLink))
                    {
                        return (new SourceInfo<string>(uidLink, uid), new SourceInfo<string>(display, uid), xrefSpec?.DeclairingFile);
                    }
                }

                // process topicHref then
                if (string.IsNullOrEmpty(topicHref))
                {
                    return (topicHref, default, default);
                }

                var topicHrefType = GetHrefType(topicHref);
                Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

                var (error, link, resolvedFile) = context.DependencyResolver.ResolveLink(topicHref, filePath, rootPath);
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

        private static TableOfContentsItem GetFirstItemWithHref(List<TableOfContentsItem> nestedTocItems)
        {
            if (nestedTocItems is null || !nestedTocItems.Any())
            {
                return null;
            }

            foreach (var nestedTocItem in nestedTocItems)
            {
                if (!string.IsNullOrEmpty(nestedTocItem.Href))
                {
                    return nestedTocItem;
                }
            }

            foreach (var nestedTocItem in nestedTocItems)
            {
                var item = GetFirstItemWithHref(nestedTocItem.Items);

                if (!string.IsNullOrEmpty(item.Href))
                {
                    return item;
                }
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
            AbsolutePath,
            RelativeFile,
            RelativeFolder,
            TocFile,
        }
    }
}
