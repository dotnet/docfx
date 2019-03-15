// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        private readonly HashSet<Document> _tocs;

        private readonly HashSet<Document> _experimentalTocs;

        private readonly IReadOnlyDictionary<Document, HashSet<Document>> _documentToTocs;

        private readonly IReadOnlyDictionary<Document, HashSet<Document>> _tocToTocs;

        private static readonly char[] wordSplitChars = new char[] { '-', '#', '_', ' ', '/' };

        public TableOfContentsMap(
            List<Document> tocs,
            List<Document> experimentalTocs,
            Dictionary<Document, HashSet<Document>> documentToTocs,
            Dictionary<Document, HashSet<Document>> tocToTocs)
        {
            _tocs = new HashSet<Document>(tocs ?? throw new ArgumentNullException(nameof(tocs)));
            _experimentalTocs = new HashSet<Document>(experimentalTocs ?? throw new ArgumentNullException(nameof(experimentalTocs)));
            _documentToTocs = documentToTocs ?? throw new ArgumentNullException(nameof(documentToTocs));
            _tocToTocs = tocToTocs ?? throw new ArgumentNullException(nameof(tocToTocs));
        }

        public bool TryFindParents(Document toc, out HashSet<Document> parents)
        {
            return _tocToTocs.TryGetValue(toc, out parents);
        }

        /// <summary>
        /// Contains toc or not
        /// </summary>
        /// <param name="toc">The toc to build</param>
        /// <returns>Whether contains toc or not</returns>
        public bool Contains(Document toc) => _tocs.Contains(toc) || _experimentalTocs.Contains(toc);

        /// <summary>
        /// Find the toc relative path to document
        /// </summary>
        /// <param name="file">Document</param>
        /// <returns>The toc relative path</returns>
        public string FindTocRelativePath(Document file)
        {
            var nearestToc = GetNearestToc(file);

            return nearestToc != null ? PathUtility.NormalizeFile(PathUtility.GetRelativePathToFile(file.SitePath, nearestToc.SitePath)) : null;
        }

        /// <summary>
        /// Return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md".
        /// when the file is not referenced, return only toc in the same or higher folder level.
        /// </summary>
        public Document GetNearestToc(Document file)
        {
            var hasReferencedTocs = false;
            var filteredTocs = (hasReferencedTocs = _documentToTocs.TryGetValue(file, out var referencedTocFiles)) ? referencedTocFiles : _tocs;

            var fileNames = Path.GetFileNameWithoutExtension(file.SitePath).Split(wordSplitChars, StringSplitOptions.RemoveEmptyEntries);
            var tocCandidates = from toc in filteredTocs
                                let dirInfo = GetRelativeDirectoryInfo(file, toc)
                                where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                                select new TocCandidate(dirInfo.subDirectoryCount, dirInfo.parentDirectoryCount, toc, fileNames);

            return tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
            {
                return CompareTocCandidate(minCandidate, nextCandidate, !file.Docset.Legacy) <= 0 ? minCandidate : nextCandidate;
            })?.Toc;
        }

        public static TableOfContentsMap Create(Context context, Docset docset)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = docset.ScanScope.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build();
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(file, builder), Progress.Update);

                return builder.Build();
            }

            void BuildTocMap(Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
            {
                try
                {
                    Debug.Assert(tocMapBuilder != null);
                    Debug.Assert(fileToBuild != null);

                    var (errors, _, referencedDocuments, referencedTocs) = context.Cache.LoadTocModel(context, fileToBuild);
                    context.Report.Write(fileToBuild.ToString(), errors);

                    tocMapBuilder.Add(fileToBuild, referencedDocuments.Select(r => r.doc), referencedTocs);
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    context.Report.Write(fileToBuild.ToString(), dex.Error);
                }
            }
        }

        private static (int subDirectoryCount, int parentDirectoryCount)
            GetRelativeDirectoryInfo(Document file, Document toc)
        {
            var relativePath = PathUtility.NormalizeFile(
                Path.GetDirectoryName(PathUtility.GetRelativePathToFile(file.SitePath, toc.SitePath)));
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

        private sealed class TocCandidate
        {
            public int SubDirectoryCount { get; }

            public int ParentDirectoryCount { get; }

            public int LevenshteinDistance => _levenshteinDistance.Value;

            public Document Toc { get; }

            public string[] FileNames { get; }

            public TocCandidate(int subDirectoryCount, int parentDirectoryCount, Document toc, string[] fileNames)
            {
                SubDirectoryCount = subDirectoryCount;
                ParentDirectoryCount = parentDirectoryCount;
                Toc = toc;
                FileNames = fileNames;
                _levenshteinDistance = new Lazy<int>(() => GetLevenshteinDistanceToFile());
            }

            // save the calculated distance during comparison to avoid repeadly calculation
            private readonly Lazy<int> _levenshteinDistance;

            private int GetLevenshteinDistanceToFile()
            {
                return Levenshtein.GetLevenshteinDistance(
                    FileNames,
                    Toc.SitePath.Split(wordSplitChars, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, possitive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest
        /// 2. parent nearest
        /// 3. sub-name word-level levenshtein distance nearest
        /// 4. sub-name lexicographical nearest
        /// </summary>
        private static int CompareTocCandidate(TocCandidate candidateX, TocCandidate candidateY, bool compareLevenshteinDistance)
        {
            var subDirCompareResult = candidateX.SubDirectoryCount - candidateY.SubDirectoryCount;
            if (subDirCompareResult != 0)
            {
                return subDirCompareResult;
            }

            var parentDirCompareResult = candidateX.ParentDirectoryCount - candidateY.ParentDirectoryCount;
            if (parentDirCompareResult != 0)
            {
                return parentDirCompareResult;
            }

            if (compareLevenshteinDistance)
            {
                var levenshteinDistanceCompareResult = candidateX.LevenshteinDistance - candidateY.LevenshteinDistance;
                if (levenshteinDistanceCompareResult != 0)
                {
                    return levenshteinDistanceCompareResult;
                }
            }

            var sitePathCompareResult = StringComparer.OrdinalIgnoreCase.Compare(candidateX.Toc.SitePath, candidateY.Toc.SitePath);
            if (!(sitePathCompareResult == 0))
            {
                return sitePathCompareResult;
            }

            return StringComparer.OrdinalIgnoreCase.Compare(candidateX.Toc.FilePath, candidateY.Toc.FilePath);
        }
    }
}
