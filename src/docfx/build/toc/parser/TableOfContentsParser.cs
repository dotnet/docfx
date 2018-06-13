// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TableOfContentsParser
    {
        public delegate string ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion);

        public static List<TableOfContentsItem> Load(string tocContent, bool isYaml, Document filePath, ResolveContent resolveContent = null, ResolveHref resolveHref = null, List<Document> parents = null)
            => LoadInputModelItems(tocContent, isYaml, filePath, filePath, resolveContent, resolveHref)?.Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r)).ToList();

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

        public static List<TableOfContentsInputItem> LoadYamlTocModel(string tocContent, string filePath)
        {
            if (string.IsNullOrEmpty(tocContent))
            {
                throw new ArgumentNullException(nameof(tocContent));
            }

            JToken tocToken;
            try
            {
                tocToken = YamlUtility.Deserialize(tocContent);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"{filePath} is not a valid TOC file, detail: {ex.Message}.", ex);
            }

            var tocInputModels = (List<TableOfContentsInputItem>)null;
            if (tocToken is JArray tocArray)
            {
                // toc model
                tocInputModels = tocArray.ToObject<List<TableOfContentsInputItem>>();
            }
            else
            {
                // toc root model
                if (tocToken is JObject tocObject &&
                    tocObject.TryGetValue("Items", out var tocItemToken) &&
                    tocItemToken is JArray tocItemArray)
                {
                    tocInputModels = tocItemArray.ToObject<List<TableOfContentsInputItem>>();
                }
            }

            if (tocInputModels != null)
            {
                return tocInputModels;
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file.");
        }

        private static List<TableOfContentsInputItem> LoadInputModelItems(string tocContent, bool isYaml, Document filePath, Document rootPath = default, ResolveContent resolveContent = null, ResolveHref resolveHref = null, List<Document> parents = null)
        {
            // TODO: support TOC.json
            parents = parents ?? new List<Document>();

            // add to parent path
            if (parents.Contains(filePath))
            {
                throw Errors.CircularReference(filePath, parents);
            }

            parents.Add(filePath);
            var models = isYaml ? LoadYamlTocModel(tocContent, filePath.FilePath) : LoadMdTocModel(tocContent, filePath.FilePath);

            if (models != null && models.Any())
            {
                ResolveTocModelItems(models, parents, filePath, rootPath, resolveContent, resolveHref);
                parents.RemoveAt(parents.Count - 1);
            }

            return models;
        }

        // tod: uid support
        private static void ResolveTocModelItems(List<TableOfContentsInputItem> tocModelItems, List<Document> parents, Document filePath, Document rootPath = default, ResolveContent resolveContent = null, ResolveHref resolveHref = null)
        {
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    ResolveTocModelItems(tocModelItem.Items, parents, filePath, rootPath, resolveContent, resolveHref);
                }

                var topicHref = tocModelItem.TopicHref;
                if (!string.IsNullOrEmpty(topicHref))
                {
                    topicHref = resolveHref?.Invoke(filePath, topicHref, rootPath) ?? topicHref;
                }

                var href = tocModelItem.Href;
                if (!string.IsNullOrEmpty(href))
                {
                    var hrefType = GetHrefType(href);
                    var (hrefPath, fragment, query) = HrefUtility.SplitHref(href);
                    if (IsIncludeHref(hrefType) && (!string.IsNullOrEmpty(fragment) || !string.IsNullOrEmpty(query)))
                    {
                        // '#' and '?' is not allowed when referencing toc file
                        href = hrefPath;
                    }

                    if (IsIncludeHref(hrefType))
                    {
                        var (referencedTocContent, referenceTocFilePath, isYamlToc) = ResolveTocHrefContent(hrefType, tocModelItem.Href, filePath, resolveContent);
                        if (referencedTocContent != null)
                        {
                            var nestedTocItems = LoadInputModelItems(referencedTocContent, isYamlToc, referenceTocFilePath, rootPath, resolveContent, resolveHref, parents);
                            if (hrefType == TocHrefType.RelativeFolder)
                            {
                                tocModelItem.Href = (string.IsNullOrEmpty(topicHref) ? GetFirstHref(nestedTocItems) : topicHref) ?? href;
                            }
                            else
                            {
                                tocModelItem.Items = nestedTocItems;
                                tocModelItem.Href = topicHref;
                            }
                        }
                    }
                    else
                    {
                        tocModelItem.Href = string.IsNullOrEmpty(topicHref)
                            ? resolveHref?.Invoke(filePath, href, rootPath) ?? tocModelItem.Href
                            : topicHref;
                    }
                }
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

        private static (string content, Document filePath, bool isYaml) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath, ResolveContent resolveContent)
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
                        return (ymlTocContent, ymlTocPath, true);
                    }

                    var mdTocHref = Path.Combine(href, "toc.md");
                    var (mdTocContent, mdTocPath) = resolveContent(filePath, mdTocHref, false);
                    return (mdTocContent, mdTocPath, false);
                case TocHrefType.MarkdownTocFile:
                    var (mc, mp) = resolveContent(filePath, href, true);
                    return (mc, mp, false);
                case TocHrefType.YamlTocFile:
                    var (yc, yp) = resolveContent(filePath, href, true);
                    return (yc, yp, true);
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
