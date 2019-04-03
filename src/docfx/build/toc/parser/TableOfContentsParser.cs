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

        public delegate (string resolvedTopicHref, Document file) ResolveHref(Document relativeTo, string href, Document resultRelativeTo, List<Range> ranges);

        public delegate (string resolvedTopicHref, string resolvedTopicName, Document file) ResolveXref(Document relativeTo, string uid, List<Range> ranges);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion, List<Range> ranges);

        public static (List<Error> errors, TableOfContentsModel model)
            Load(Context context, Document file, ResolveContent resolveContent, ResolveHref resolveHref, ResolveXref resolveXref)
        {
            return LoadInternal(context, file, file, resolveContent, resolveHref, resolveXref, new List<Document>());
        }

        private static (List<Error> errors, TableOfContentsModel tocModel, Dictionary<string, List<Range>> hrefLineInfoMap) LoadTocModel(Context context, Document file, string content = null)
        {
            var filePath = file.FilePath;

            if (file.IsFromHistory)
            {
                Debug.Assert(!string.IsNullOrEmpty(content));
            }

            if (filePath.EndsWith(".yml", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content is null ? YamlUtility.Deserialize(file, context) : YamlUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                var hrefLineInfoMap = BuildLineInfoMap(tocToken);
                return (errors, toc, hrefLineInfoMap);
            }
            else if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                var (errors, tocToken) = content is null ? JsonUtility.Deserialize(file, context) : JsonUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                var hrefLineInfoMap = BuildLineInfoMap(tocToken);
                return (errors, toc, hrefLineInfoMap);
            }
            else if (filePath.EndsWith(".md", PathUtility.PathComparison))
            {
                content = content ?? file.ReadText();
                GitUtility.CheckMergeConflictMarker(content, file.FilePath);
                var (errors, toc) = MarkdownTocMarkup.LoadMdTocModel(content, file, context);

                // TODO: build href line info map for toc.md
                return (errors, toc, new Dictionary<string, List<Range>>());
            }

            throw new NotSupportedException($"{filePath} is an unknown TOC file");
        }

        private static Dictionary<string, List<Range>> BuildLineInfoMap(JToken tocToken, Dictionary<string, List<Range>> hrefLineInfoMap = null)
        {
            if (hrefLineInfoMap is null)
            {
                hrefLineInfoMap = new Dictionary<string, List<Range>>();
            }

            if (tocToken is JArray)
            {
                foreach (var item in tocToken.Children())
                {
                    BuildLineInfoMap(item, hrefLineInfoMap);
                }
            }
            else if (tocToken is JObject obj)
            {
                foreach (var item in tocToken.Children())
                {
                    var prop = item as JProperty;
                    if (prop.Value is JObject)
                    {
                        BuildLineInfoMap(prop.Value, hrefLineInfoMap);
                    }
                    else if (prop.Value is JArray)
                    {
                        foreach (var i in prop.Value.Children())
                        {
                            BuildLineInfoMap(i, hrefLineInfoMap);
                        }
                    }
                    else if (prop.Value is JValue)
                    {
                        var key = $"{prop.Name.ToLowerInvariant()}+{prop.Value}";
                        if (hrefLineInfoMap.TryGetValue(key, out var value))
                        {
                            value.Add(JsonUtility.ToRange(prop));
                        }
                        else
                        {
                            hrefLineInfoMap.Add(key, new List<Range> { JsonUtility.ToRange(prop) });
                        }
                    }
                }
            }
            return hrefLineInfoMap;
        }

        private static (List<Error>, TableOfContentsModel) LoadTocModel(JToken tocToken)
        {
            if (tocToken is JArray tocArray)
            {
                // toc model
                var (errors, items) = JsonUtility.ToObjectWithSchemaValidation<List<TableOfContentsItem>>(tocArray);
                return (errors, new TableOfContentsModel
                {
                    Items = items,
                });
            }
            else if (tocToken is JObject tocObject)
            {
                // toc root model
                return JsonUtility.ToObjectWithSchemaValidation<TableOfContentsModel>(tocToken);
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

            var (errors, model, lineInfoMap) = LoadTocModel(context, file, content);

            if (model.Items.Count > 0)
            {
                parents.Add(file);
                errors.AddRange(ResolveTocModelItems(context, model.Items, parents, file, rootPath, resolveContent, resolveHref, resolveXref, lineInfoMap));
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
            ResolveXref resolveXref,
            Dictionary<string, List<Range>> lineInfoMap)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, resolveContent, resolveHref, resolveXref, lineInfoMap));
                }

                var tocHref = GetTocHref(tocModelItem);
                var (topicHrefName, topicHref) = GetTopicHref(tocModelItem);
                var topicUid = tocModelItem.Uid;

                var (resolvedTocHref, resolvedTopicItemFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, resolvedTopicName, document) = ProcessTopicItem(topicUid, topicHrefName, topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTocHref ?? resolvedTopicHref ?? resolvedTopicItemFromTocHref?.Href;
                tocModelItem.TocHref = resolvedTocHref;
                tocModelItem.Name = tocModelItem.Name ?? resolvedTopicName;
                tocModelItem.Items = subChildren?.Items ?? tocModelItem.Items;
            }

            return errors;

            string GetTocHref(TableOfContentsItem tocInputModel)
            {
                var key = $"{nameof(tocInputModel.TocHref).ToLowerInvariant()}+{tocInputModel.TocHref}";
                lineInfoMap.TryGetValue(key, out var ranges);

                if (!string.IsNullOrEmpty(tocInputModel.TocHref))
                {
                    var tocHrefType = GetHrefType(tocInputModel.TocHref);
                    if (IsIncludeHref(tocHrefType) || tocHrefType == TocHrefType.AbsolutePath)
                    {
                        return tocInputModel.TocHref;
                    }
                    else
                    {
                        errors.AddRange(JsonUtility.IncludeAll(ranges, Errors.InvalidTocHref(filePath, tocInputModel.TocHref, default)));
                    }
                }

                if (!string.IsNullOrEmpty(tocInputModel.Href) && IsIncludeHref(GetHrefType(tocInputModel.Href)))
                {
                    return tocInputModel.Href;
                }

                return default;
            }

            (string name, string topicHref) GetTopicHref(TableOfContentsItem tocInputModel)
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
                        return (nameof(tocInputModel.TopicHref).ToLowerInvariant(), tocInputModel.TopicHref);
                    }
                }

                if (string.IsNullOrEmpty(tocInputModel.Href) || !IsIncludeHref(GetHrefType(tocInputModel.Href)))
                {
                    return (nameof(tocInputModel.Href).ToLowerInvariant(), tocInputModel.Href);
                }

                return default;
            }

            (string resolvedTocHref, TableOfContentsItem resolvedTopicItem, TableOfContentsModel subChildren) ProcessTocHref(string tocHref)
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

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent, lineInfoMap);
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

            (string resolvedTopicHref, string resolvedTopicName, Document file) ProcessTopicItem(string uid, string topicHrefName, string topicHref)
            {
                var key = $"{topicHrefName}+{topicHref}";
                lineInfoMap.TryGetValue(key, out var ranges);

                // process uid first
                if (!string.IsNullOrEmpty(uid))
                {
                    var (uidHref, uidDisplayName, uidFile) = resolveXref.Invoke(rootPath, uid, ranges);
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

                var (resolvedTopicHref, file) = resolveHref.Invoke(filePath, topicHref, rootPath, ranges);
                return (resolvedTopicHref, null, file);
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

        private static (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath, ResolveContent resolveContent, Dictionary<string, List<Range>> lineInfoMap)
        {
            var key = $"href+{href}";
            lineInfoMap.TryGetValue(key, out var ranges);

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
                    return resolveContent(filePath, href, isInclusion: true, ranges);
                default:
                    return default;
            }

            (string content, Document filePath)? Resolve(string name)
            {
                var content = resolveContent(filePath, Path.Combine(href, name), isInclusion: false, ranges);
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
