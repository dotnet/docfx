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

        public delegate (string resolvedTopicHref, Document file) ResolveHref(Document relativeTo, SourceInfo<string> href, Document resultRelativeTo);

        public delegate (string resolvedTopicHref, string resolvedTopicName, Document file) ResolveXref(Document relativeTo, SourceInfo<string> uid);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, SourceInfo<string> href, bool isInclusion);

        public static (List<Error> errors, TableOfContentsModel model)
            Load(Context context, Document file, ResolveContent resolveContent, ResolveHref resolveHref, ResolveXref resolveXref)
        {
            return LoadInternal(context, file, file, resolveContent, resolveHref, resolveXref, new List<Document>());
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
                return MarkdownTocMarkup.LoadMdTocModel(content, file, context);
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
                return JsonUtility.ToObject<TableOfContentsModel>(tocToken);
            }
            return (new List<Error>(), new TableOfContentsModel());
        }

        private static (List<Error> errors, TableOfContentsModel model) LoadInternal(
            Context context,
            Document file,
            Document rootPath,
            ResolveContent resolveContent,
            ResolveHref resolveHref,
            ResolveXref resolveXref,
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
                errors.AddRange(ResolveTocModelItems(context, model.Items, parents, file, rootPath, resolveContent, resolveHref, resolveXref));
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
            ResolveContent resolveContent,
            ResolveHref resolveHref,
            ResolveXref resolveXref)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, resolveContent, resolveHref, resolveXref));
                }

                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, resolvedTopicItemFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(topicUid, topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTocHref ?? resolvedTopicHref ?? resolvedTopicItemFromTocHref?.Href;
                tocModelItem.TocHref = resolvedTocHref;
                tocModelItem.Name = tocModelItem.Name ?? resolvedTopicName;
                tocModelItem.Items = subChildren?.Items ?? tocModelItem.Items;
            }

            return errors;

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

                var (hrefPath, fragment, query) = HrefUtility.SplitHref(tocHref);
                tocHref.Value = hrefPath;

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent);
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedToc) = LoadInternal(context, referenceTocFilePath, rootPath, resolveContent, resolveHref, resolveXref, parents, referencedTocContent);
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

            (SourceInfo<string> resolvedTopicHref, string resolvedTopicName, Document file) ProcessTopicItem(SourceInfo<string> uid, SourceInfo<string> topicHref)
            {
                // process uid first
                if (!string.IsNullOrEmpty(uid))
                {
                    var (uidHref, uidDisplayName, uidFile) = resolveXref.Invoke(rootPath, uid);
                    if (!string.IsNullOrEmpty(uidHref))
                    {
                        uid.Value = uidHref;
                        return (uid, uidDisplayName, uidFile);
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
                topicHref.Value = resolvedTopicHref;
                return (topicHref, null, file);
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

        private static (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, SourceInfo<string> href, Document filePath, ResolveContent resolveContent)
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
                var content = resolveContent(filePath, new SourceInfo<string>(Path.Combine(href, name), href), isInclusion: false);
                if (content.file != null)
                {
                    return content;
                }
                return null;
            }
        }

        private static TocHrefType GetHrefType(string href)
        {
            var hrefType = HrefUtility.GetHrefType(href);
            if (hrefType == HrefType.AbsolutePath || hrefType == HrefType.External)
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
