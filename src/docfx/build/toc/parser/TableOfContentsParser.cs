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
        internal static List<TableOfContentsModel> Load(string tocContent, bool isYaml, Document filePath = default, Document rootPath = default, ResolveContent resolveContent = null, ResolveLink resolveLink = null)
        {
            var inputModels = isYaml ? LoadYamlTocModel(tocContent, filePath) : LoadMdTocModel(tocContent);

            var outputModels = inputModels?.Select(t => TableOfContentsItem.ToTableOfContentsModel(t)).ToList();
            if (outputModels != null && outputModels.Any())
            {
                // todo: recursive loop check
                ResolveTocModelItems(outputModels, filePath, rootPath, resolveContent, resolveLink);
            }

            return outputModels;
        }

        internal static List<TableOfContentsItem> LoadMdTocModel(string tocContent)
        {
            var content = tocContent.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase).Replace("\r", "\n", StringComparison.OrdinalIgnoreCase);
            TableOfContentsParseState state = new InitialState();
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

        internal static List<TableOfContentsItem> LoadYamlTocModel(string tocContent, Document filePath)
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

            if (tocToken is JArray tocArray)
            {
                // toc model
                return tocArray.ToObject<List<TableOfContentsItem>>();
            }
            else
            {
                // toc root model
                if (tocToken is JObject tocObject &&
                    tocObject.TryGetValue("Items", out var tocItemToken) &&
                    tocItemToken is JArray tocItemArray)
                {
                    return tocItemArray.ToObject<List<TableOfContentsItem>>();
                }
            }

            throw new NotSupportedException($"{filePath} is not a valid TOC file.");
        }

        // todo: resolve topic href to href
        // tod: uid support
        private static void ResolveTocModelItems(List<TableOfContentsModel> tocModelItems, Document filePath = default, Document rootPath = default, ResolveContent resolveContent = null, ResolveLink resolveLink = null)
        {
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Children != null && tocModelItem.Children.Any())
                {
                    ResolveTocModelItems(tocModelItem.Children, filePath, rootPath, resolveContent, resolveLink);
                }

                var href = tocModelItem.Href;
                if (!string.IsNullOrEmpty(href))
                {
                    var hrefType = GetHrefType(href);
                    var (hrefPath, fragment, query) = HrefUtility.SplitHref(href);
                    if ((hrefType == TocHrefType.MarkdownTocFile || hrefType == TocHrefType.YamlTocFile || hrefType == TocHrefType.RelativeFolder) &&
                        (!string.IsNullOrEmpty(fragment) || !string.IsNullOrEmpty(query)))
                    {
                        // '#' and '?' is not allowed when referencing toc file
                        href = hrefPath;
                    }

                    if (IsIncludeLink(hrefType))
                    {
                        var (referencedTocContent, referenceTocFilePath, isYamlToc) = ResolveTocHrefContent(hrefType, tocModelItem.Href, filePath, resolveContent);
                        if (referencedTocContent != null)
                        {
                            tocModelItem.Children = Load(referencedTocContent, isYamlToc, referenceTocFilePath, rootPath, resolveContent, resolveLink);
                            tocModelItem.Href = null;
                        }
                    }
                    else
                    {
                        tocModelItem.Href = resolveLink?.Invoke(filePath, href, rootPath) ?? tocModelItem.Href;
                    }
                }
            }
        }

        private static bool IsIncludeLink(TocHrefType tocHrefType)
        {
            return tocHrefType == TocHrefType.MarkdownTocFile ||
                tocHrefType == TocHrefType.YamlTocFile ||
                tocHrefType == TocHrefType.RelativeFolder;
        }

        private static (string content, Document filePath, bool isYaml) ResolveTocHrefContent(TocHrefType tocHrefType, string href, Document filePath = default, ResolveContent resolveContent = null)
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
                    var (ymlTocContent, ymlTocPath) = resolveContent(filePath, ymlTocHref);

                    if (ymlTocPath != null)
                    {
                        return (ymlTocContent, ymlTocPath, true);
                    }

                    var mdTocHref = Path.Combine(href, "toc.md");
                    var (mdTocContent, mdTocPath) = resolveContent(filePath, mdTocHref);
                    return (mdTocContent, mdTocPath, false);
                case TocHrefType.MarkdownTocFile:
                    var (mc, mp) = resolveContent(filePath, href);
                    return (mc, mp, false);
                case TocHrefType.YamlTocFile:
                    var (yc, yp) = resolveContent(filePath, href);
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
