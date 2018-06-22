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

        public static List<TableOfContentsItem> Load(string tocContent, Document filePath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            return LoadInputModelItems(tocContent, filePath, filePath, resolveContent, resolveHref, new List<Document>())
                .Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r))
                .ToList();
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

            throw new NotSupportedException($"{filePath} is must be an array or an object with an items property");
        }

        private static List<TableOfContentsInputItem> LoadInputModelItems(string tocContent, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref, List<Document> parents)
        {
            // add to parent path
            if (parents.Contains(filePath))
            {
                throw Errors.CircularReference(filePath, parents).ToException();
            }

            parents.Add(filePath);

            var models = LoadTocModel(tocContent, filePath.FilePath);

            if (models.Any())
            {
                ResolveTocModelItems(models, parents, filePath, rootPath, resolveContent, resolveHref);
                parents.RemoveAt(parents.Count - 1);
            }

            return models;
        }

        // tod: uid support
        private static void ResolveTocModelItems(List<TableOfContentsInputItem> tocModelItems, List<Document> parents, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref)
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
                    topicHref = resolveHref.Invoke(filePath, topicHref, rootPath);
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
                        var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(hrefType, tocModelItem.Href, filePath, resolveContent);
                        if (referencedTocContent != null)
                        {
                            var nestedTocItems = LoadInputModelItems(referencedTocContent, referenceTocFilePath, rootPath, resolveContent, resolveHref, parents);
                            if (hrefType == TocHrefType.RelativeFolder)
                            {
                                tocModelItem.Href = string.IsNullOrEmpty(topicHref) ? GetFirstHref(nestedTocItems) : topicHref;
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
                        tocModelItem.Href = string.IsNullOrEmpty(topicHref) ? resolveHref.Invoke(filePath, href, rootPath) : topicHref;
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
            return tocHrefType == TocHrefType.TocFile || tocHrefType == TocHrefType.RelativeFolder;
        }

        private static (string content, Document filePath) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath, ResolveContent resolveContent)
        {
            switch (tocHrefType)
            {
                case TocHrefType.RelativeFolder:
                    return Resolve("toc.yml") ?? Resolve("toc.json") ?? Resolve("toc.md") ?? default;
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
            var fileName = Path.GetFileName(href);
            if (string.IsNullOrEmpty(fileName))
            {
                return TocHrefType.RelativeFolder;
            }

            if ("toc.md".Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                "toc.json".Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                "toc.yml".Equals(fileName, StringComparison.OrdinalIgnoreCase))
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
