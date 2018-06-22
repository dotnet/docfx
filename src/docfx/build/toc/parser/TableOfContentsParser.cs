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
        public delegate string ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion);

        public static (List<TableOfContentsItem> model, List<DocfxException> errors) Load(string tocContent, Document filePath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var (inputModel, errors) = LoadInputModelItems(tocContent, filePath, filePath, resolveContent, resolveHref);
            return (inputModel?.Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r)).ToList(), errors);
        }

        public static List<TableOfContentsInputItem> LoadMdTocModel(string tocContent, string filePath)
        {
            var content = tocContent.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase).Replace("\r", "\n", StringComparison.OrdinalIgnoreCase);
            TableOfContentsParseState state = new InitialState(filePath);
            var rules = new TableOfContentsParseRule[]
            {
                new TopicTocParseRule(),
                new ExternalLinkTocParseRule(),
                new ContainerParseRule(),
                new CommentParseRule(),
                new WhitespaceParseRule(),
            };

            int lineNumber = 1;
            while (content.Length > 0)
            {
                state = state.ApplyRules(rules, ref content, ref lineNumber);
            }

            return state.Root;
        }

        private static List<TableOfContentsInputItem> LoadTocModel(string content, string filePath)
        {
            if (filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                JToken tocToken;
                try
                {
                    tocToken = YamlUtility.Deserialize(content);
                }
                catch (Exception ex)
                {
                    throw new NotSupportedException($"{filePath} is not a valid TOC file, detail: {ex.Message}.", ex);
                }
                return LoadTocModel(tocToken, filePath);
            }
            else if (filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                JToken tocToken;
                try
                {
                    tocToken = JToken.Parse(content);
                }
                catch (Exception ex)
                {
                    throw new NotSupportedException($"{filePath} is not a valid TOC file, detail: {ex.Message}.", ex);
                }
                return LoadTocModel(tocToken, filePath);
            }
            else if (filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return LoadMdTocModel(content, filePath);
            }

            throw new NotSupportedException($"{filePath} is an unknown TOC file");
        }

        private static List<TableOfContentsInputItem> LoadTocModel(JToken tocToken, string filePath)
        {
            if (tocToken is JArray tocArray)
            {
                // toc model
                return tocArray.ToObject<List<TableOfContentsInputItem>>();
            }
            else
            {
                // toc root model
                if (tocToken is JObject tocObject &&
                    tocObject.TryGetValue("Items", out var tocItemToken) &&
                    tocItemToken is JArray tocItemArray)
                {
                    return tocItemArray.ToObject<List<TableOfContentsInputItem>>();
                }
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file.");
        }

        private static (List<TableOfContentsInputItem> model, List<DocfxException> errors) LoadInputModelItems(string tocContent, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref, List<Document> parents = null)
        {
            parents = parents ?? new List<Document>();

            // add to parent path
            if (parents.Contains(filePath))
            {
                throw Errors.CircularReference(filePath, parents);
            }

            parents.Add(filePath);

            var errors = new List<DocfxException>();
            var models = LoadTocModel(tocContent, filePath.FilePath);

            if (models != null && models.Any())
            {
                errors = ResolveTocModelItems(models, parents, filePath, rootPath, resolveContent, resolveHref);
                parents.RemoveAt(parents.Count - 1);
            }

            return (models, errors);
        }

        // tod: uid support
        private static List<DocfxException> ResolveTocModelItems(List<TableOfContentsInputItem> tocModelItems, List<Document> parents, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var errors = new List<DocfxException>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(tocModelItem.Items, parents, filePath, rootPath, resolveContent, resolveHref));
                }

                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);

                var (resolvedTocHref, resolvedTopicHrefFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var resolvedTopicHref = ProcessTopicHref(topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTopicHref ?? resolvedTopicHrefFromTocHref;
                tocModelItem.TocHref = resolvedTocHref;
                if (subChildren != null)
                {
                    tocModelItem.Items = subChildren;
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
                        errors.Add(Errors.InvalidTocHref(tocInputModel.TocHref));
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

            (string resolvedTocHref, string resolvedTopicHref, List<TableOfContentsInputItem> subChildren) ProcessTocHref(string tocHref)
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
                if (!string.IsNullOrEmpty(fragment) || !string.IsNullOrEmpty(query))
                {
                    // '#' and '?' is not allowed when referencing toc file
                    tocHref = hrefPath;
                }
                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent);
                if (referencedTocContent != null)
                {
                    var (nestedTocItems, subErrors) = LoadInputModelItems(referencedTocContent, referenceTocFilePath, rootPath, resolveContent, resolveHref, parents);
                    errors.AddRange(subErrors);
                    if (tocHrefType == TocHrefType.RelativeFolder)
                    {
                        return (default, GetFirstHref(nestedTocItems), default);
                    }
                    else
                    {
                        return (default, default, nestedTocItems);
                    }
                }

                return default;
            }

            string ProcessTopicHref(string topicHref)
            {
                if (string.IsNullOrEmpty(topicHref))
                {
                    return topicHref;
                }

                var topicHrefType = GetHrefType(topicHref);
                Debug.Assert(topicHrefType == TocHrefType.AbsolutePath || !IsIncludeHref(topicHrefType));

                return resolveHref.Invoke(filePath, topicHref, rootPath);
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
            return tocHrefType == TocHrefType.MarkdownTocFile ||
                tocHrefType == TocHrefType.YamlTocFile ||
                tocHrefType == TocHrefType.RelativeFolder;
        }

        private static (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath, ResolveContent resolveContent)
        {
            if (resolveContent == null)
            {
                return default;
            }

            switch (tocHrefType)
            {
                case TocHrefType.RelativeFolder:
                    // First, try finding toc.yml under the relative folder
                    // Second, try finding toc.md under the relative folder
                    var ymlTocHref = Path.Combine(href, "toc.yml");
                    var (ymlTocContent, ymlTocPath) = resolveContent(filePath, ymlTocHref, false);

                    if (ymlTocPath != null)
                    {
                        return (ymlTocContent, ymlTocPath);
                    }

                    var mdTocHref = Path.Combine(href, "toc.md");
                    var (mdTocContent, mdTocPath) = resolveContent(filePath, mdTocHref, false);
                    return (mdTocContent, mdTocPath);
                case TocHrefType.MarkdownTocFile:
                    var (mc, mp) = resolveContent(filePath, href, true);
                    return (mc, mp);
                case TocHrefType.YamlTocFile:
                    var (yc, yp) = resolveContent(filePath, href, true);
                    return (yc, yp);
                default:
                    // do nothing
                    break;
            }

            return default;
        }

        private static TocHrefType GetHrefType(string href)
        {
            if (!HrefUtility.IsRelativeHref(href))
            {
                return TocHrefType.AbsolutePath;
            }
            var fileName = Path.GetFileName(href);
            if (string.IsNullOrEmpty(fileName))
            {
                return TocHrefType.RelativeFolder;
            }

            if ("toc.md".Equals(Path.GetFileName(href), StringComparison.OrdinalIgnoreCase))
            {
                return TocHrefType.MarkdownTocFile;
            }

            if ("toc.yml".Equals(Path.GetFileName(href), StringComparison.OrdinalIgnoreCase))
            {
                return TocHrefType.YamlTocFile;
            }

            return TocHrefType.RelativeFile;
        }

        private enum TocHrefType
        {
            AbsolutePath,
            RelativeFile,
            RelativeFolder,
            MarkdownTocFile,
            YamlTocFile,
        }
    }
}
