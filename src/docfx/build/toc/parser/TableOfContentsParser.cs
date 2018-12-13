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

        public delegate (Error error, string href, Document file) ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (Error error, string content, Document file) ResolveContent(Document relativeTo, string href);

        public static (List<Error> errors, List<TableOfContentsItem> items, JObject metadata)
            Load(Context context, Document file, MonikersProvider monikersProvider, Dictionary<Document, List<string>> monikerMap, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var (errors, inputModel) = LoadInputModelItems(context, file, file, monikerMap, resolveContent, resolveHref, new List<Document>());

            var items = inputModel?.Items?.Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r, monikersProvider.Comparer)).ToList();

            return (errors, items, inputModel?.Metadata);
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
            else
            {
                // toc root model
                if (tocToken is JObject tocObject)
                {
                    return JsonUtility.ToObjectWithSchemaValidation<TableOfContentsInputModel>(tocToken);
                }
            }
            return (new List<Error>(), new TableOfContentsInputModel());
        }

        private static (List<Error> errors, TableOfContentsInputModel model) LoadInputModelItems(
            Context context,
            Document file,
            Document rootPath,
            Dictionary<Document, List<string>> monikerMap,
            ResolveContent resolveContent,
            ResolveHref resolveHref,
            List<Document> parents,
            string content = null)
        {
            // add to parent path
            if (parents.Contains(file))
            {
                throw Errors.CircularReference(file, parents).ToException();
            }

            parents.Add(file);

            var (errors, models) = LoadTocModel(context, file, content);

            if (models.Items.Any())
            {
                errors.AddRange(ResolveTocModelItems(context, models.Items, parents, file, rootPath, monikerMap, resolveContent, resolveHref));
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, models);
        }

        // todo: uid support
        private static List<Error> ResolveTocModelItems(
            Context context,
            List<TableOfContentsInputItem> tocModelItems,
            List<Document> parents,
            Document filePath,
            Document rootPath,
            Dictionary<Document, List<string>> monikerMap,
            ResolveContent resolveContent,
            ResolveHref resolveHref)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, monikerMap, resolveContent, resolveHref));
                }

                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);

                var (resolvedTocHref, resolvedTopicHrefFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicFile) = ProcessTopicHref(topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTopicHref ?? resolvedTopicHrefFromTocHref;
                tocModelItem.TocHref = resolvedTocHref;

                if (resolvedTopicFile != null && monikerMap != null && monikerMap.TryGetValue(resolvedTopicFile, out var monikers))
                {
                    tocModelItem.Monikers = monikers;
                }

                if (subChildren != null)
                {
                    tocModelItem.Items = subChildren.Items;
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

            (string resolvedTocHref, string resolvedTopicHref, TableOfContentsInputModel subChildren) ProcessTocHref(string tocHref)
            {
                if (string.IsNullOrEmpty(tocHref))
                {
                    return (tocHref, default, default);
                }

                var tocHrefType = GetHrefType(tocHref);
                Debug.Assert(tocHrefType == TocHrefType.AbsolutePath || IsIncludeHref(tocHrefType));

                switch (tocHrefType)
                {
                    case TocHrefType.AbsolutePath:
                        return (tocHref, default, default);

                    case TocHrefType.RelativeFolder:
                        var (hrefPath, _, _) = HrefUtility.SplitHref(tocHref);

                        foreach (var tocFileName in s_tocFileNames)
                        {
                            var (_, includeContent, includeTocFile) = resolveContent(filePath, Path.Combine(hrefPath, tocFileName));
                            if (includeTocFile == null)
                            {
                                continue;
                            }

                            var (loadErrors, loadedToc) = LoadInputModelItems(
                                context,
                                includeTocFile,
                                rootPath,
                                monikerMap,
                                resolveContent,
                                resolveHref,
                                parents,
                                includeContent);

                            errors.AddRange(loadErrors);

                            return (default, GetFirstHref(loadedToc.Items), default);
                        }

                        // TODO: should we warn if we cannot find any TOC file inside that folder?
                        return default;

                    case TocHrefType.TocFile:
                        var (error, content, includeFile) = resolveContent(filePath, tocHref);
                        errors.AddIfNotNull(error);

                        if (includeFile == null)
                        {
                            return default;
                        }

                        var (subErrors, nestedToc) = LoadInputModelItems(
                            context,
                            includeFile,
                            rootPath,
                            monikerMap,
                            resolveContent,
                            resolveHref,
                            parents,
                            content);

                        errors.AddRange(subErrors);
                        return (default, default, nestedToc);

                    default:
                        return default;
                }
            }

            (string resolvedTopicHref, Document file) ProcessTopicHref(string topicHref)
            {
                if (string.IsNullOrEmpty(topicHref))
                {
                    return (topicHref, default);
                }

                var topicHrefType = GetHrefType(topicHref);
                Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

                var (error, href, file) = resolveHref.Invoke(filePath, topicHref, rootPath);
                errors.AddIfNotNull(error);

                return (href, file);
            }
        }

        private static string GetFirstHref(List<TableOfContentsInputItem> nestedTocItems)
        {
            if (nestedTocItems == null || !nestedTocItems.Any())
            {
                return null;
            }

            foreach (var nestedTocItem in nestedTocItems)
            {
                if (!string.IsNullOrEmpty(nestedTocItem.Href))
                {
                    return nestedTocItem.Href;
                }
            }

            foreach (var nestedTocItem in nestedTocItems)
            {
                var href = GetFirstHref(nestedTocItem.Items);

                if (!string.IsNullOrEmpty(href))
                {
                    return href;
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
