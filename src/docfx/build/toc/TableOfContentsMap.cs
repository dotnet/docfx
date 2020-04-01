// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between toc and document
    /// </summary>
    internal class TableOfContentsMap
    {
        private readonly ErrorLog _errorLog;
        private readonly BuildScope _buildScope;
        private readonly TableOfContentsLoader _tocLoader;
        private readonly DocumentProvider _documentProvider;

        private readonly Lazy<(Dictionary<Document, Document[]> tocToTocs, Dictionary<Document, Document[]> docToTocs)> _tocs;

        public TableOfContentsMap(ErrorLog errorLog, BuildScope buildScope, TableOfContentsLoader tocLoader, DocumentProvider documentProvider)
        {
            _errorLog = errorLog;
            _buildScope = buildScope;
            _tocLoader = tocLoader;
            _documentProvider = documentProvider;
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

            return nearestToc != null ? PathUtility.NormalizeFile(PathUtility.GetRelativePathToFile(file.SitePath, nearestToc.SitePath)) : null;
        }

        /// <summary>
        /// Return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md".
        /// when the file is not referenced, return only toc in the same or higher folder level.
        /// </summary>
        public Document? FindNearestToc(Document file)
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
                    result = minCandidate.parentCount - nextCandidate.parentCount;
                if (result == 0)
                    result = minCandidate.toc.CompareTo(nextCandidate.toc);
                return result <= 0 ? minCandidate : nextCandidate;
            }).toc;
        }

        private static (int subDirectoryCount, int parentDirectoryCount) GetRelativeDirectoryInfo(string pathA, string pathB)
        {
            var relativePath = PathUtility.NormalizeFile(
                Path.GetDirectoryName(PathUtility.GetRelativePathToFile(pathA, pathB)) ?? "");
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
                var tocReferences = new ConcurrentDictionary<Document, (List<Document> docs, List<Document> tocs)>();

                ParallelUtility.ForEach(
                    _buildScope.GetFiles(ContentType.TableOfContents),
                    file => LoadToc(file, tocReferences),
                    Progress.Update);

                var tocToTocs = new Dictionary<Document, Document[]>();
                var includedTocs = tocReferences.Values.SelectMany(item => item.tocs).ToHashSet();

                foreach (var (toc, (docs, tocs)) in tocReferences)
                {
                    if (includedTocs.Contains(toc))
                    {
                        // TOC been included by other TOCs will be ignored
                        continue;
                    }

                    tocToTocs.Add(toc, tocs.Distinct().ToArray());
                }

                var docToTocs = (
                    from item in tocReferences
                    from doc in item.Value.docs
                    where tocToTocs.ContainsKey(item.Key) && !item.Key.IsExperimental
                    group item.Key by doc).ToDictionary(g => g.Key, g => g.Distinct().ToArray());

                return (tocToTocs, docToTocs);
            }
        }

        private void LoadToc(FilePath path, ConcurrentDictionary<Document, (List<Document> files, List<Document> tocs)> tocReferences)
        {
            try
            {
                var file = _documentProvider.GetDocument(path);
                Debug.Assert(file.ContentType == ContentType.TableOfContents);

                var (errors, _, referencedDocuments, referencedTocs) = _tocLoader.Load(file);
                _errorLog.Write(errors);

                tocReferences.TryAdd(file, (referencedDocuments, referencedTocs));
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                _errorLog.Write(dex);
            }
        }
    }
}
