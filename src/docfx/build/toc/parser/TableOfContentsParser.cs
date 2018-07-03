// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class TableOfContentsParser
    {
        public delegate string ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion);

        public static (List<Error> errors, List<TableOfContentsItem> model) Load(string tocContent, Document filePath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var (errors, inputModel) = LoadInputModelItems(tocContent, filePath, filePath, resolveContent, resolveHref, new List<Document>());

            return (errors, inputModel?.Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r)).ToList());
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

        private static (List<TableOfContentsInputItem>, List<Error> errors) LoadTocModel(string content, string filePath)
        {
            var errors = new List<Error>();
            var mappings = new Dictionary<JToken, List<(int, int)>>();
            if (filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            {
                JToken tocToken;
                (tocToken, errors, mappings) = YamlUtility.Deserialize(content);

                return (LoadTocModel(tocToken, mappings, errors, false), errors);
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
                    errors.Add(Errors.JsonSyntaxError(ex));
                    return (LoadTocModel(JValue.CreateNull(), mappings, errors), errors);
                }
                return (LoadTocModel(tocToken, mappings, errors), errors);
            }
            else if (filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                return (LoadMdTocModel(content, filePath), errors);
            }

            throw new NotSupportedException($"{filePath} is an unknown TOC file");
        }

        private static JArray ValidateNullElement(this JArray tocArray, Dictionary<JToken, List<(int, int)>> mappings, List<Error> errors, bool fromJsonFile)
        {
            var nullNodes = new List<JToken>();
            foreach (var item in tocArray.Children<JObject>())
            {
                foreach (var element in item.Children())
                {
                    var prop = element as JProperty;
                    if (prop.Value.IsNullOrEmpty())
                    {
                        if (fromJsonFile)
                        {
                            var lineInfo = item as IJsonLineInfo;
                            errors.Add(Errors.NullValue(lineInfo.LineNumber, lineInfo.LinePosition));
                        }
                        else
                        {
                            if (mappings.TryGetValue(item, out List<(int, int)> value))
                            {
                                errors.AddRange(value.Select(x => Errors.NullValue(x.Item1, x.Item2)));
                            }
                        }

                        nullNodes.Add(item);
                    }
                }
            }

            foreach (var node in nullNodes)
            {
                node.Remove();
            }

            return tocArray;
        }

        private static List<TableOfContentsInputItem> LoadTocModel(
            JToken tocToken,
            Dictionary<JToken,
            List<(int, int)>> mappings,
            List<Error> errors,
            bool fromJasonFile = false)
        {
            if (tocToken is JArray tocArray)
            {
                // toc model
                var temp = tocArray.ValidateNullElement(mappings, errors, fromJasonFile).ToObject<List<TableOfContentsInputItem>>();
                return temp;
            }
            else
            {
                // toc root model
                if (tocToken is JObject tocObject &&
                    tocObject.TryGetValue("items", out var tocItemToken) &&
                    tocItemToken is JArray tocItemArray)
                {
                    var temp = tocItemArray.ValidateNullElement(mappings, errors, fromJasonFile).ToObject<List<TableOfContentsInputItem>>();
                    return temp;
                }
            }
            return new List<TableOfContentsInputItem>();
        }

        private static (List<Error> errors, List<TableOfContentsInputItem> model) LoadInputModelItems(string tocContent, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref, List<Document> parents)
        {
            // add to parent path
            if (parents.Contains(filePath))
            {
                throw Errors.CircularReference(filePath, parents).ToException();
            }

            parents.Add(filePath);

            var (models, errors) = LoadTocModel(tocContent, filePath.FilePath);

            if (models.Any())
            {
                errors.AddRange(ResolveTocModelItems(models, parents, filePath, rootPath, resolveContent, resolveHref));
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, models);
        }

        // tod: uid support
        private static List<Error> ResolveTocModelItems(List<TableOfContentsInputItem> tocModelItems, List<Document> parents, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var errors = new List<Error>();
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
                tocHref = hrefPath;

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent);
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedTocItems) = LoadInputModelItems(referencedTocContent, referenceTocFilePath, rootPath, resolveContent, resolveHref, parents);
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

            var (path, _, _) = HrefUtility.SplitHref(href);
            if (path.EndsWith('/') || path.EndsWith('\\'))
            {
                return TocHrefType.RelativeFolder;
            }

            var fileName = Path.GetFileName(path);

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
