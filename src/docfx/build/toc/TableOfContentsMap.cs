// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        public TableOfContentsMap(List<Document> tocs, List<Document> experimentalTocs, Dictionary<Document, HashSet<Document>> documentToTocs)
        {
            _tocs = new HashSet<Document>(tocs ?? throw new ArgumentNullException(nameof(tocs)));
            _experimentalTocs = new HashSet<Document>(experimentalTocs ?? throw new ArgumentNullException(nameof(experimentalTocs)));
            _documentToTocs = documentToTocs ?? throw new ArgumentNullException(nameof(documentToTocs));
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
        /// i.e. relativePath only contains "..".
        /// </summary>
        public Document GetNearestToc(Document file)
        {
            var hasReferencedTocs = false;
            var filteredTocs = (hasReferencedTocs = _documentToTocs.TryGetValue(file, out var referencedTocFiles)) ? referencedTocFiles : _tocs;

            // when there's no referenced toc, sub directory count should be zero (only look in parent folders)
            var tocCandidates = from toc in filteredTocs
                                let dirInfo = GetRelativeDirectoryInfo(file, toc)
                                where hasReferencedTocs || dirInfo.parentDirectoryCount >= dirInfo.subDirectoryCount
                                select new TocCandidate(dirInfo.subDirectoryCount, dirInfo.parentDirectoryCount, toc);

            return tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
            {
                return CompareTocCandidate(minCandidate, nextCandidate, file) <= 0 ? minCandidate : nextCandidate;
            })?.Toc;
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

            // save the calculated distance during comparison to avoid repeadly calculation
            public int? LevenshteinDistance { get; set; }

            public Document Toc { get; }

            public TocCandidate(int subDirectoryCount, int parentDirectoryCount, Document toc)
            {
                SubDirectoryCount = subDirectoryCount;
                ParentDirectoryCount = parentDirectoryCount;
                Toc = toc;
            }
        }

        /**
         * Compare two toc candidate relative to target file.
         * Return -1 if x is closer than y, 1 if x is farer than y, 0 if x equals y.
         */
        private static int CompareTocCandidate(TocCandidate candidateX, TocCandidate candidateY, Document targetFile)
        {
            // from referenced pick the nearest one
            // 1. sub nearest
            // 2. parent nearest
            // 3. sub-name word-level levenshtein distance nearest
            // 4. sub-name lexicographical nearest
            var subDirCompareResult = Comparer<int>.Default.Compare(candidateX.SubDirectoryCount, candidateY.SubDirectoryCount);
            if (subDirCompareResult == 0)
            {
                var parentDirCompareResult = Comparer<int>.Default.Compare(candidateX.ParentDirectoryCount, candidateY.ParentDirectoryCount);
                if (parentDirCompareResult == 0)
                {
                    var fileNames = Path.GetFileNameWithoutExtension(targetFile.SitePath).Split(new[] { '-', '#', '_' })
                        .Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();
                    candidateX.LevenshteinDistance = candidateX.LevenshteinDistance.HasValue ? candidateX.LevenshteinDistance.Value :
                        Levenshtein.GetLevenshteinDistance(fileNames, candidateX.Toc.SitePath.Split(new[] { '-', '#', '_', '/' }), StringComparer.OrdinalIgnoreCase);
                    candidateY.LevenshteinDistance = candidateY.LevenshteinDistance.HasValue ? candidateY.LevenshteinDistance.Value :
                        Levenshtein.GetLevenshteinDistance(fileNames, candidateY.Toc.SitePath.Split(new[] { '-', '#', '_', '/' }), StringComparer.OrdinalIgnoreCase);

                    var levenshteinDistanceCompareResult = Comparer<int>.Default.Compare(candidateX.LevenshteinDistance.Value, candidateY.LevenshteinDistance.Value);
                    return levenshteinDistanceCompareResult == 0 ? StringComparer.OrdinalIgnoreCase.Compare(candidateX.Toc.SitePath, candidateY.Toc.SitePath)
                        : levenshteinDistanceCompareResult;
                }
                else
                {
                    return parentDirCompareResult;
                }
            }
            else
            {
                return subDirCompareResult;
            }
        }
    }
}
