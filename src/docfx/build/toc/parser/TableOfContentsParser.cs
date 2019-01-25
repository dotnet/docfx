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

        public delegate (string resolvedTopicHref, Document file) ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (string resolvedTopicHref, string resolvedTopicName, Document file) ResolveXref(Document relativeTo, string uid);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion);

        public static (List<Error> errors, List<TableOfContentsItem> items, TableOfContentsMetadata metadata)
            Load(Context context, Document file, MonikerMap monikerMap, ResolveContent resolveContent, ResolveHref resolveHref, ResolveXref resolveXref)
        {
            var (errors, inputModel, tocMetadata) = LoadInputModel(context, file, file, monikerMap, resolveContent, resolveHref, resolveXref, new List<Document>());

            var items = inputModel?.Items?.Select(r => r.ToTableOfContentsModel()).ToList();
            return (errors, items, tocMetadata);
        }

        private static (List<Error> errors, TableOfContentsInputModel tocModel) LoadTocModel(Context context, Document file, string content = null)
        {
            var filePath = file.FilePath;

            if (file.IsFromHistory)
            {
                Debug.Assert(!string.IsNullOrEmpty(content));
            }

            if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content == null ? YamlUtility.Deserialize(file, context) : YamlUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content == null ? JsonUtility.Deserialize(file, context) : JsonUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".md", PathUtility.PathComparison))
            {
                content = content ?? file.ReadText();
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                return MarkdownTocMarkup.LoadMdTocModel(content, file, context);
            }

            throw new NotSupportedException($"{filePath} is an unknown TOC file");
        }

        private static (List<Error>, TableOfContentsInputModel) LoadTocModel(JToken tocToken)
        {
            if (tocToken is JArray tocArray)
            {
                // toc model
                var (errors, items) = JsonUtility.ToObjectWithSchemaValidation<List<TableOfContentsInputItem>>(tocArray);
                return (errors, new TableOfContentsInputModel
                {
                    Items = items,
                });
            }
            else if (tocToken is JObject tocObject)
            {
                // toc root model
                return JsonUtility.ToObjectWithSchemaValidation<TableOfContentsInputModel>(tocToken);
            }
            return (new List<Error>(), new TableOfContentsInputModel());
        }

        private static (List<Error> errors, TableOfContentsInputModel model, TableOfContentsMetadata metadata) LoadInputModel(
            Context context,
            Document file,
            Document rootPath,
            MonikerMap monikerMap,
            ResolveContent resolveContent,
            ResolveHref resolveHref,
            ResolveXref resolveXref,
            List<Document> parents,
            List<string> fileMonikers = null,
            string content = null)
        {
            // add to parent path
            if (parents.Contains(file))
            {
                parents.Add(file);
                throw Errors.CircularReference(parents).ToException();
            }

            var (errors, model) = LoadTocModel(context, file, content);

            TableOfContentsMetadata tocMetadata = null;
            if (parents.Count == 0)
            {
                Error monikerError;
                var (metadataErrors, metadata) = context.MetadataProvider.GetMetadata(file, model.Metadata);
                errors.AddRange(metadataErrors);

                tocMetadata = metadata.ToObject<TableOfContentsMetadata>();
                (monikerError, tocMetadata.Monikers) = context.MonikerProvider.GetFileLevelMonikers(file, tocMetadata.MonikerRange);
                errors.AddIfNotNull(monikerError);

                fileMonikers = tocMetadata.Monikers;
            }

            if (model.Items.Count > 0)
            {
                parents.Add(file);
                errors.AddRange(ResolveTocModelItems(context, model.Items, parents, file, rootPath, monikerMap, resolveContent, resolveHref, resolveXref, fileMonikers));
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, model, tocMetadata);
        }

        private static List<Error> ResolveTocModelItems(
            Context context,
            List<TableOfContentsInputItem> tocModelItems,
            List<Document> parents,
            Document filePath,
            Document rootPath,
            MonikerMap monikerMap,
            ResolveContent resolveContent,
            ResolveHref resolveHref,
            ResolveXref resolveXref,
            List<string> fileMonikers)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, monikerMap, resolveContent, resolveHref, resolveXref, fileMonikers));
                }

                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, resolvedTopicItemFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(topicUid, topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTopicHref ?? resolvedTopicItemFromTocHref?.Href;
                tocModelItem.TocHref = resolvedTocHref;
                tocModelItem.Name = tocModelItem.Name ?? resolvedTopicName;
                if (subChildren != null)
                {
                    tocModelItem.Items = subChildren.Items;
                }
                if (monikerMap != null)
                {
                    tocModelItem.Monikers = ResolveMoniker(document, tocModelItem, monikerMap, context.MonikerProvider.Comparer, fileMonikers, resolvedTopicHref, resolvedTopicItemFromTocHref);
                }
            }

            return errors;

            string GetTocHref(TableOfContentsInputItem tocInputModel)
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
                        errors.Add(Errors.InvalidTocHref(filePath, tocInputModel.TocHref));
                    }
                }

                if (!string.IsNullOrEmpty(tocInputModel.Href) && IsIncludeHref(GetHrefType(tocInputModel.Href)))
                {
                    return tocInputModel.Href;
                }

                return default;
            }

            string GetTopicHref(TableOfContentsInputItem tocInputModel)
            {
                if (!string.IsNullOrEmpty(tocInputModel.TopicHref))
                {
                    var topicHrefType = GetHrefType(tocInputModel.TopicHref);
                    if (IsIncludeHref(topicHrefType))
                    {
                        errors.Add(Errors.InvalidTopicHref(filePath, tocInputModel.TopicHref));
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

            (string resolvedTocHref, TableOfContentsInputItem resolvedTopicItem, TableOfContentsInputModel subChildren) ProcessTocHref(string tocHref)
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

                var (hrefPath, fragment, query) = HrefUtility.SplitHref(tocHref);
                tocHref = hrefPath;

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent);
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedToc, _) = LoadInputModel(context, referenceTocFilePath, rootPath, monikerMap, resolveContent, resolveHref, resolveXref, parents, fileMonikers, referencedTocContent);
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

            (string resolvedTopicHref, string resolvedTopicName, Document file) ProcessTopicItem(string uid, string topicHref)
            {
                // process uid first
                if (!string.IsNullOrEmpty(uid))
                {
                    var (uidHref, uidDisplayName, uidFile) = resolveXref.Invoke(rootPath, uid);
                    if (!string.IsNullOrEmpty(uidHref))
                    {
                        return (uidHref, uidDisplayName, uidFile);
                    }
                }

                // process topicHref then
                if (string.IsNullOrEmpty(topicHref))
                {
                    return (topicHref, null, null);
                }

                var topicHrefType = GetHrefType(topicHref);
                Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

                var (resolvedTopicHref, file) = resolveHref.Invoke(filePath, topicHref, rootPath);
                return (resolvedTopicHref, null, file);
            }
        }

        private static TableOfContentsInputItem GetFirstItemWithHref(List<TableOfContentsInputItem> nestedTocItems)
        {
            if (nestedTocItems == null || !nestedTocItems.Any())
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

        private static List<string> ResolveMoniker(
            Document document,
            TableOfContentsInputItem tocModelItem,
            MonikerMap monikerMap,
            MonikerComparer comparer,
            List<string> fileMonikers,
            string resolvedTopicHref,
            TableOfContentsInputItem resolvedTopicItemFromTocHref)
        {
            List<string> monikers = null;
            if (resolvedTopicHref != null)
            {
                if (document != null)
                {
                    monikerMap.TryGetValue(document, out monikers);
                }
                else if (tocModelItem.Href != null && GetHrefType(tocModelItem.Href) == TocHrefType.AbsolutePath)
                {
                    monikers = fileMonikers;
                }
            }
            else
            {
                monikers = resolvedTopicItemFromTocHref?.Monikers;
            }

            var childrenMonikers = tocModelItem.Items?.SelectMany(child => child?.Monikers ?? new List<string>()) ?? new List<string>();
            monikers = childrenMonikers.Union(monikers ?? new List<string>()).Distinct(comparer).ToList();
            monikers.Sort(comparer);
            return monikers;
        }

        private static bool IsIncludeHref(TocHrefType tocHrefType)
        {
            return tocHrefType == TocHrefType.TocFile || tocHrefType == TocHrefType.RelativeFolder;
        }

        private static (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath, ResolveContent resolveContent)
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
                    return resolveContent(filePath, href, isInclusion: true);
                default:
                    return default;
            }

            (string content, Document filePath)? Resolve(string name)
            {
                var content = resolveContent(filePath, Path.Combine(href, name), isInclusion: false);
                if (content.file != null)
                {
                    return content;
                }
                return null;
            }
        }

        private static TocHrefType GetHrefType(string href)
        {
            if (HrefUtility.IsAbsoluteHref(href))
            {
                return TocHrefType.AbsolutePath;
            }

            var (path, _, _) = HrefUtility.SplitHref(href);
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
