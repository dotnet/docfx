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

        public delegate (string resolvedTopicHref, List<string> monikers) ResolveHref(Document relativeTo, string href, Document resultRelativeTo);

        public delegate (string content, Document file) ResolveContent(Document relativeTo, string href, bool isInclusion);

        public static (List<Error> errors, List<TableOfContentsItem> items, JObject metadata, List<string> monikers) Load(Context context, Document file, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var (errors, inputModel) = LoadInputModelItems(context, file, file, resolveContent, resolveHref, new List<Document>());

            var items = inputModel?.Items?.Select(r => TableOfContentsInputItem.ToTableOfContentsModel(r, file.Docset.Monikers.Comparer)).ToList();
            var fileMonikers = items?.SelectMany(r => r.Monikers).Distinct(file.Docset.Monikers.Comparer).ToList();
            fileMonikers.Sort(file.Docset.Monikers.Comparer);
            return (errors, items, inputModel?.Metadata, fileMonikers);
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
                var (errors, tocToken) = !file.IsFromHistory ? YamlUtility.Deserialize(file, context) : YamlUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".json", PathUtility.PathComparison))
            {
                var (errors, tocToken) = !file.IsFromHistory ? JsonUtility.Deserialize(file, context) : JsonUtility.Deserialize(content);
                var (loadErrors, toc) = LoadTocModel(tocToken);
                errors.AddRange(loadErrors);
                return (errors, toc);
            }
            else if (filePath.EndsWith(".md", PathUtility.PathComparison))
            {
                content = file.IsFromHistory ? content : file.ReadText();
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

        private static (List<Error> errors, TableOfContentsInputModel model) LoadInputModelItems(Context context, Document file, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref, List<Document> parents, string content = null)
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
                errors.AddRange(ResolveTocModelItems(context, models.Items, parents, file, rootPath, resolveContent, resolveHref));
                parents.RemoveAt(parents.Count - 1);
            }

            return (errors, models);
        }

        // todo: uid support
        private static List<Error> ResolveTocModelItems(Context context, List<TableOfContentsInputItem> tocModelItems, List<Document> parents, Document filePath, Document rootPath, ResolveContent resolveContent, ResolveHref resolveHref)
        {
            var errors = new List<Error>();
            foreach (var tocModelItem in tocModelItems)
            {
                if (tocModelItem.Items != null && tocModelItem.Items.Any())
                {
                    errors.AddRange(ResolveTocModelItems(context, tocModelItem.Items, parents, filePath, rootPath, resolveContent, resolveHref));
                }

                var tocHref = GetTocHref(tocModelItem);
                var topicHref = GetTopicHref(tocModelItem);

                var (resolvedTocHref, resolvedTopicHrefFromTocHref, subChildren) = ProcessTocHref(tocHref);
                var (resolvedTopicHref, monikers) = ProcessTopicHref(topicHref);

                // set resolved href back
                tocModelItem.Href = resolvedTopicHref ?? resolvedTopicHrefFromTocHref;
                tocModelItem.TocHref = resolvedTocHref;
                tocModelItem.Monikers = monikers;
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

                if (tocHrefType == TocHrefType.AbsolutePath)
                {
                    return (tocHref, default, default);
                }

                var (hrefPath, fragment, query) = HrefUtility.SplitHref(tocHref);
                tocHref = hrefPath;

                var (referencedTocContent, referenceTocFilePath) = ResolveTocHrefContent(tocHrefType, tocHref, filePath, resolveContent);
                if (referencedTocContent != null)
                {
                    var (subErrors, nestedToc) = LoadInputModelItems(context, referenceTocFilePath, rootPath, resolveContent, resolveHref, parents, referencedTocContent);
                    errors.AddRange(subErrors);
                    if (tocHrefType == TocHrefType.RelativeFolder)
                    {
                        return (default, GetFirstHref(nestedToc.Items), default);
                    }
                    else
                    {
                        return (default, default, nestedToc);
                    }
                }

                return default;
            }

            (string resolvedTopicHref, List<string> monikers) ProcessTopicHref(string topicHref)
            {
                if (string.IsNullOrEmpty(topicHref))
                {
                    return (topicHref, new List<string>());
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
