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
        private readonly Config _config;
        private readonly Input _input;
        private readonly ErrorBuilder _errors;
        private readonly BuildScope _buildScope;
        private readonly TableOfContentsLoader _tocLoader;
        private readonly TableOfContentsParser _tocParser;
        private readonly DocumentProvider _documentProvider;
        private readonly DependencyMapBuilder _dependencyMapBuilder;
        private readonly ContentValidator _contentValidator;

        private readonly Lazy<(Dictionary<FilePath, FilePath[]> tocToTocs, Dictionary<FilePath, FilePath[]> docToTocs, List<FilePath> servicePages)> _tocs;

        public TableOfContentsMap(
            Config config,
            ErrorBuilder errors,
            Input input,
            BuildScope buildScope,
            DependencyMapBuilder dependencyMapBuilder,
            TableOfContentsParser tocParser,
            TableOfContentsLoader tocLoader,
            DocumentProvider documentProvider,
            ContentValidator contentValidator)
        {
            _config = config;
            _errors = errors;
            _input = input;
            _buildScope = buildScope;
            _tocParser = tocParser;
            _tocLoader = tocLoader;
            _documentProvider = documentProvider;
            _dependencyMapBuilder = dependencyMapBuilder;
            _contentValidator = contentValidator;
            _tocs = new Lazy<(Dictionary<FilePath, FilePath[]>, Dictionary<FilePath, FilePath[]>, List<FilePath>)>(BuildTocMap);
        }

        public IEnumerable<FilePath> GetFiles()
        {
            return _tocs.Value.tocToTocs.Keys.Where(ShouldBuildFile).Concat(_tocs.Value.servicePages);
        }

        /// <summary>
        /// Find the toc relative path to document
        /// </summary>
        /// <param name="file">Document</param>
        /// <returns>The toc relative path</returns>
        public string? FindTocRelativePath(FilePath file)
        {
            var nearestToc = FindNearestToc(file);
            if (nearestToc is null)
            {
                return null;
            }

            _dependencyMapBuilder.AddDependencyItem(file, nearestToc, DependencyType.Metadata);
            return UrlUtility.GetRelativeUrl(_documentProvider.GetSiteUrl(file), _documentProvider.GetSiteUrl(nearestToc));
        }

        /// <summary>
        /// Return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md".
        /// when the file is not referenced, return only toc in the same or higher folder level.
        /// </summary>
        internal FilePath? FindNearestToc(FilePath file)
        {
            var result = FindNearestToc(
                file,
                _tocs.Value.tocToTocs.Keys,
                _tocs.Value.docToTocs,
                file => file.Path);

            _contentValidator.ValidateTocMissing(file, result.hasReferencedTocs);
            return result.toc;
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, positive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest(based on file path)
        /// 2. parent nearest(based on file path)
        /// 3. sub-name lexicographical nearest
        /// </summary>
        internal static (T? toc, bool hasReferencedTocs) FindNearestToc<T>(
            T file, IEnumerable<T> tocs, Dictionary<T, T[]> documentsToTocs, Func<T, string> getPath) where T : class, IComparable<T>
        {
            var hasReferencedTocs = false;
            var filteredTocs = (hasReferencedTocs = documentsToTocs.TryGetValue(file, out var referencedTocFiles)) ? referencedTocFiles : tocs;

            var tocCandidates = from toc in filteredTocs
                                let dirInfo = GetRelativeDirectoryInfo(getPath(file), getPath(toc))
                                where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                                select (subCount: dirInfo.subDirectoryCount, parentCount: dirInfo.parentDirectoryCount, toc);

            return (tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
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
            }).toc, hasReferencedTocs);
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

        private bool ShouldBuildFile(FilePath file)
        {
            if (file.Origin != FileOrigin.Fallback)
            {
                return true;
            }

            // if A toc includes B toc and only B toc is localized, then A need to be included and built
            if (_tocs.Value.tocToTocs.TryGetValue(file, out var tocReferences) && tocReferences.Any(toc => toc.Origin != FileOrigin.Fallback))
            {
                return true;
            }

            return false;
        }

        private (Dictionary<FilePath, FilePath[]> tocToTocs, Dictionary<FilePath, FilePath[]> docToTocs, List<FilePath> servicePages)
            BuildTocMap()
        {
            using (Progress.Start("Loading TOC"))
            {
                var tocs = new ConcurrentBag<FilePath>();
                var allServicePages = new ConcurrentBag<FilePath>();

                // Parse and split TOC
                ParallelUtility.ForEach(_errors, _buildScope.GetFiles(ContentType.TableOfContents), file =>
                {
                    SplitToc(file, _tocParser.Parse(file, _errors), tocs);
                });

                var tocReferences = new ConcurrentDictionary<FilePath, (List<FilePath> docs, List<FilePath> tocs)>();

                // Load TOC
                ParallelUtility.ForEach(_errors, tocs, file =>
                {
                    var (_, referencedDocuments, referencedTocs, servicePages) = _tocLoader.Load(file);

                    tocReferences.TryAdd(file, (referencedDocuments, referencedTocs));

                    foreach (var servicePage in servicePages)
                    {
                        allServicePages.Add(servicePage);
                    }
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
                    where tocToTocs.ContainsKey(item.Key) && !item.Key.IsExperimental()
                    group item.Key by doc).ToDictionary(g => g.Key, g => g.Distinct().ToArray());

                tocToTocs.TrimExcess();
                docToTocs.TrimExcess();

                return (tocToTocs, docToTocs, allServicePages.ToList());
            }
        }

        private void SplitToc(FilePath file, TableOfContentsNode toc, ConcurrentBag<FilePath> result)
        {
            if (!Array.Exists(_config.SplitTOC, e => e.Equals(file.Path.Value)) || toc.Items.Count <= 0)
            {
                result.Add(file);
                return;
            }

            var newToc = new TableOfContentsNode(toc);

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
                var name = newNodeToken.TryGetValue<JValue>("name", out var splitByValue) ? splitByValue.ToString() : null;
                if (string.IsNullOrEmpty(name))
                {
                    newToc.Items.Add(item);
                    continue;
                }

                var newNodeFilePath = new PathString(Path.Combine(Path.GetDirectoryName(file.Path) ?? "", $"_splitted/{name}/TOC.yml"));
                var newNodeFile = FilePath.Generated(newNodeFilePath);

                _input.AddGeneratedContent(newNodeFile, new JArray { newNodeToken }, null);
                result.Add(newNodeFile);

                var newChild = new TableOfContentsNode(child)
                {
                    Href = child.Href.With($"_splitted/{name}/"),
                };

                newToc.Items.Add(new SourceInfo<TableOfContentsNode>(newChild, item.Source));
            }

            var newTocFilePath = new PathString(Path.ChangeExtension(file.Path, ".yml"));
            var newTocFile = FilePath.Generated(newTocFilePath);
            _input.AddGeneratedContent(newTocFile, JsonUtility.ToJObject(newToc), null);
            result.Add(newTocFile);
        }

        private TableOfContentsNode SplitTocNode(TableOfContentsNode node)
        {
            var newNode = new TableOfContentsNode(node)
            {
                TopicHref = node.TopicHref.With(FixHref(node.TopicHref)),
                TocHref = node.TopicHref.With(FixHref(node.TocHref)),
                Href = node.TopicHref.With(FixHref(node.Href)),
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
