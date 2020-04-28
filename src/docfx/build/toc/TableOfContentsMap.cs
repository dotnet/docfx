// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between toc and document
    /// </summary>
    internal class TableOfContentsMap
    {
        private readonly Input _input;
        private readonly ErrorLog _errorLog;
        private readonly BuildScope _buildScope;
        private readonly TableOfContentsLoader _tocLoader;
        private readonly TableOfContentsParser _tocParser;
        private readonly DocumentProvider _documentProvider;
        private readonly DependencyMapBuilder _dependencyMapBuilder;

        private readonly Lazy<(Dictionary<Document, Document[]> tocToTocs, Dictionary<Document, Document[]> docToTocs)> _tocs;

        public TableOfContentsMap(
            ErrorLog errorLog, Input input, BuildScope buildScope, DependencyMapBuilder dependencyMapBuilder, TableOfContentsParser tocParser, TableOfContentsLoader tocLoader, DocumentProvider documentProvider)
        {
            _errorLog = errorLog;
            _input = input;
            _buildScope = buildScope;
            _tocParser = tocParser;
            _tocLoader = tocLoader;
            _documentProvider = documentProvider;
            _dependencyMapBuilder = dependencyMapBuilder;
            _tocs = new Lazy<(Dictionary<Document, Document[]> tocToTocs, Dictionary<Document, Document[]> docToTocs)>(BuildTocMap);
        }

        public IEnumerable<FilePath> GetFiles()
        {
            return _tocs.Value.tocToTocs.Keys.Where(ShouldBuildFile).Select(toc => toc.FilePath);
        }

        /// <summary>
        /// Find the toc relative path to document
        /// </summary>
        /// <param name="file">Document</param>
        /// <returns>The toc relative path</returns>
        public string? FindTocRelativePath(Document file)
        {
            var nearestToc = FindNearestToc(file);
            if (nearestToc is null)
            {
                return null;
            }

            _dependencyMapBuilder.AddDependencyItem(file, nearestToc, DependencyType.Metadata);
            return PathUtility.NormalizeFile(PathUtility.GetRelativePathToFile(file.SitePath, nearestToc.SitePath));
        }

        /// <summary>
        /// Return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md".
        /// when the file is not referenced, return only toc in the same or higher folder level.
        /// </summary>
        internal Document? FindNearestToc(Document file)
        {
            return FindNearestToc(
                file,
                _tocs.Value.tocToTocs.Keys.Where(toc => !toc.IsExperimental),
                _tocs.Value.docToTocs,
                file => file.FilePath.Path);
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, positive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest(based on file path)
        /// 2. parent nearest(based on file path)
        /// 3. sub-name lexicographical nearest
        /// </summary>
        internal static T? FindNearestToc<T>(T file, IEnumerable<T> tocs, Dictionary<T, T[]> documentsToTocs, Func<T, string> getPath) where T : class, IComparable<T>
        {
            var hasReferencedTocs = false;
            var filteredTocs = (hasReferencedTocs = documentsToTocs.TryGetValue(file, out var referencedTocFiles)) ? referencedTocFiles : tocs;

            var tocCandidates = from toc in filteredTocs
                                let dirInfo = GetRelativeDirectoryInfo(getPath(file), getPath(toc))
                                where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                                select (subCount: dirInfo.subDirectoryCount, parentCount: dirInfo.parentDirectoryCount, toc);

            return tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
            {
                var result = minCandidate.subCount - nextCandidate.subCount;
                if (result == 0)
                {
                    result = minCandidate.parentCount - nextCandidate.parentCount;
                }
                if (result == 0)
                {
                    result = minCandidate.toc.CompareTo(nextCandidate.toc);
                }
                return result <= 0 ? minCandidate : nextCandidate;
            }).toc;
        }

        private static (int subDirectoryCount, int parentDirectoryCount) GetRelativeDirectoryInfo(string pathA, string pathB)
        {
            var relativePath = PathUtility.NormalizeFile(Path.GetDirectoryName(PathUtility.GetRelativePathToFile(pathA, pathB)) ?? "");
            if (string.IsNullOrEmpty(relativePath))
            {
                return default;
            }

            // todo: perf optimization, don't split '/' here again.
            var relativePathParts = relativePath.Split('/').Where(path => !string.IsNullOrWhiteSpace(path));
            var parentDirectoryCount = 0;
            var subDirectoryCount = 0;

            foreach (var part in relativePathParts)
            {
                switch (part)
                {
                    case "..":
                        parentDirectoryCount++;
                        break;
                    default:
                        break;
                }
            }

            subDirectoryCount = relativePathParts.Count() - parentDirectoryCount;
            return (subDirectoryCount, parentDirectoryCount);
        }

        private bool ShouldBuildFile(Document file)
        {
            if (file.FilePath.Origin != FileOrigin.Fallback)
            {
                return true;
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            if (_tocs.Value.tocToTocs.TryGetValue(file, out var tocReferences) && tocReferences.Any(toc => toc.FilePath.Origin != FileOrigin.Fallback))
            {
                return true;
            }

            return false;
        }

        private (Dictionary<Document, Document[]> tocToTocs, Dictionary<Document, Document[]> docToTocs) BuildTocMap()
        {
            using (Progress.Start("Loading TOC"))
            {
                var tocs = new ConcurrentBag<FilePath>();

                // Parse and split TOC
                ParallelUtility.ForEach(_errorLog, _buildScope.GetFiles(ContentType.TableOfContents), file =>
                {
                    var errors = new List<Error>();
                    var toc = _tocParser.Parse(file, errors);
                    _errorLog.Write(errors);

                    SplitToc(file, toc, tocs);
                });

                var tocReferences = new ConcurrentDictionary<Document, (List<Document> docs, List<Document> tocs)>();

                // Load TOC
                ParallelUtility.ForEach(_errorLog, tocs, path =>
                {
                    var file = _documentProvider.GetDocument(path);
                    var (errors, _, referencedDocuments, referencedTocs) = _tocLoader.Load(file);
                    _errorLog.Write(errors);

                    tocReferences.TryAdd(file, (referencedDocuments, referencedTocs));
                });

                // Create TOC reference map
                var includedTocs = tocReferences.Values.SelectMany(item => item.tocs).ToHashSet();

                var tocToTocs = (
                    from item in tocReferences
                    where !includedTocs.Contains(item.Key)
                    select item).ToDictionary(g => g.Key, g => g.Value.tocs.Distinct().ToArray());

                var docToTocs = (
                    from item in tocReferences
                    from doc in item.Value.docs
                    where tocToTocs.ContainsKey(item.Key) && !item.Key.IsExperimental
                    group item.Key by doc).ToDictionary(g => g.Key, g => g.Distinct().ToArray());

                return (tocToTocs, docToTocs);
            }
        }

        private void SplitToc(FilePath file, TableOfContentsNode toc, ConcurrentBag<FilePath> result)
        {
            if (string.IsNullOrEmpty(toc.SplitItemsBy) || toc.Items.Count <= 0)
            {
                result.Add(file);
                return;
            }

            var newToc = new TableOfContentsNode(toc) { Items = new List<SourceInfo<TableOfContentsNode>>() };

            foreach (var item in toc.Items)
            {
                var child = item.Value;
                if (child.Items.Count == 0)
                {
                    newToc.Items.Add(item);
                    continue;
                }

                var newNode = SplitTocNode(child);
                var newNodeToken = JsonUtility.ToJObject(newNode);
                var name = newNodeToken.TryGetValue<JValue>(toc.SplitItemsBy, out var splitByValue) ? splitByValue.ToString() : null;
                if (string.IsNullOrEmpty(name))
                {
                    newToc.Items.Add(item);
                    continue;
                }

                var newNodeFilePath = new PathString(Path.Combine(Path.GetDirectoryName(file.Path) ?? "", $"_splitted/{name}/TOC.yml"));
                var newNodeFile = FilePath.Generated(newNodeFilePath);

                _input.AddGeneratedContent(newNodeFile, new JArray { newNodeToken });
                result.Add(newNodeFile);

                var newChild = new TableOfContentsNode(child)
                {
                    Href = child.Href.With($"_splitted/{name}/"),
                    Items = new List<SourceInfo<TableOfContentsNode>>(),
                };

                newToc.Items.Add(new SourceInfo<TableOfContentsNode>(newChild, item.Source));
            }

            var newTocFilePath = new PathString(Path.ChangeExtension(file.Path, ".json"));
            var newTocFile = FilePath.Generated(newTocFilePath);
            _input.AddGeneratedContent(newTocFile, JsonUtility.ToJObject(newToc));
            result.Add(newTocFile);
        }

        private TableOfContentsNode SplitTocNode(TableOfContentsNode node)
        {
            var newNode = new TableOfContentsNode(node)
            {
                TopicHref = node.TopicHref.With(FixHref(node.TopicHref)),
                TocHref = node.TopicHref.With(FixHref(node.TocHref)),
                Href = node.TopicHref.With(FixHref(node.Href)),
                Items = new List<SourceInfo<TableOfContentsNode>>(),
            };

            foreach (var item in node.Items)
            {
                newNode.Items.Add(item.With(SplitTocNode(item)));
            }

            return newNode;

            static string? FixHref(string? href)
            {
                return href != null && UrlUtility.GetLinkType(href) == LinkType.RelativePath ? Path.Combine("../../", href) : href;
            }
        }
    }
}
