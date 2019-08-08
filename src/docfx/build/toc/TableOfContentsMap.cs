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

        private readonly IReadOnlyDictionary<Document, List<Document>> _tocReferences;

        private readonly MonikerProvider _monikerProvider;

        public TableOfContentsMap(
            List<Document> tocs,
            List<Document> experimentalTocs,
            Dictionary<Document, HashSet<Document>> documentToTocs,
            Dictionary<Document, List<Document>> tocReferences,
            MonikerProvider monikerProvider = null)
        {
            _tocs = new HashSet<Document>(tocs ?? throw new ArgumentNullException(nameof(tocs)));
            _experimentalTocs = new HashSet<Document>(experimentalTocs ?? throw new ArgumentNullException(nameof(experimentalTocs)));
            _documentToTocs = documentToTocs ?? throw new ArgumentNullException(nameof(documentToTocs));
            _tocReferences = tocReferences ?? throw new ArgumentNullException(nameof(tocReferences));
            _monikerProvider = monikerProvider;
        }

        public bool TryGetTocReferences(Document toc, out List<Document> tocs)
        {
            return _tocReferences.TryGetValue(toc, out tocs);
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

            var tocCandidates = from toc in filteredTocs
                                let dirInfo = GetRelativeDirectoryInfo(file, toc)
                                where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                                select new TocCandidate(dirInfo.subDirectoryCount, dirInfo.parentDirectoryCount, toc);

            return tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
            {
                return CompareTocCandidate(minCandidate, nextCandidate, file) <= 0 ? minCandidate : nextCandidate;
            })?.Toc;
        }

        public static TableOfContentsMap Create(Context context)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                var tocFiles = context.BuildScope.Files.Where(f => f.ContentType == ContentType.TableOfContents);
                if (!tocFiles.Any())
                {
                    return builder.Build(context.MonikerProvider);
                }

                ParallelUtility.ForEach(tocFiles, file => BuildTocMap(file, builder), Progress.Update);

                return builder.Build(context.MonikerProvider);
            }

            void BuildTocMap(Document fileToBuild, TableOfContentsMapBuilder tocMapBuilder)
            {
                try
                {
                    Debug.Assert(tocMapBuilder != null);
                    Debug.Assert(fileToBuild != null);

                    var (errors, _, referencedDocuments, referencedTocs) = context.Cache.LoadTocModel(context, fileToBuild);
                    context.ErrorLog.Write(fileToBuild, errors);

                    tocMapBuilder.Add(fileToBuild, referencedDocuments, referencedTocs);
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    context.ErrorLog.Write(fileToBuild, dex.Error, isException: true);
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

            public Document Toc { get; }

            public TocCandidate(int subDirectoryCount, int parentDirectoryCount, Document toc)
            {
                SubDirectoryCount = subDirectoryCount;
                ParentDirectoryCount = parentDirectoryCount;
                Toc = toc;
            }
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, possitive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest(based on site path)
        /// 2. parent nearest(based on site path)
        /// 3. site path
        /// 4. moniker intersection
        /// 5. file path
        /// </summary>
        private int CompareTocCandidate(TocCandidate candidateX, TocCandidate candidateY, Document file)
        {
            var result = candidateX.SubDirectoryCount - candidateY.SubDirectoryCount;
            if (result == 0)
                result = candidateX.ParentDirectoryCount - candidateY.ParentDirectoryCount;
            if (result == 0)
                result = string.Compare(candidateX.Toc.SitePath, candidateY.Toc.SitePath, PathUtility.PathComparison);
            if (result == 0 && _monikerProvider != null)
            {
                var fileMonikers = _monikerProvider.GetFileLevelMonikers(file).monikers;
                result = _monikerProvider.GetFileLevelMonikers(candidateY.Toc).monikers.Intersect(fileMonikers, StringComparer.OrdinalIgnoreCase).Count() -
                         _monikerProvider.GetFileLevelMonikers(candidateX.Toc).monikers.Intersect(fileMonikers, StringComparer.OrdinalIgnoreCase).Count();
            }
            if (result == 0)
                result = candidateX.Toc.FilePath.CompareTo(candidateY.Toc.FilePath);
            return result;
        }
    }
}
