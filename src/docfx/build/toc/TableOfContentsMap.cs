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

        public TableOfContentsMap(
            List<Document> tocs,
            List<Document> experimentalTocs,
            Dictionary<Document, HashSet<Document>> documentToTocs,
            Dictionary<Document, List<Document>> tocReferences)
        {
            _tocs = new HashSet<Document>(tocs ?? throw new ArgumentNullException(nameof(tocs)));
            _experimentalTocs = new HashSet<Document>(experimentalTocs ?? throw new ArgumentNullException(nameof(experimentalTocs)));
            _documentToTocs = documentToTocs ?? throw new ArgumentNullException(nameof(documentToTocs));
            _tocReferences = tocReferences ?? throw new ArgumentNullException(nameof(tocReferences));
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
                                let dirInfo = GetRelativeDirectoryInfo(file.SitePath, toc.SitePath)
                                where hasReferencedTocs || dirInfo.subDirectoryCount == 0 /*due breadcrumb toc*/
                                select new TocCandidate((dirInfo.subDirectoryCount, dirInfo.parentDirectoryCount), toc, file);

            return tocCandidates.DefaultIfEmpty().Aggregate((minCandidate, nextCandidate) =>
            {
                return CompareTocCandidate(minCandidate, nextCandidate) <= 0 ? minCandidate : nextCandidate;
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
            GetRelativeDirectoryInfo(string pathA, string pathB)
        {
            var relativePath = PathUtility.NormalizeFile(
                Path.GetDirectoryName(PathUtility.GetRelativePathToFile(pathA, pathB)));
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
            private Lazy<(int subDirectoryCount, int parentDirectoryCount)> _fileDirInfo;

            public (int subCount, int parentCount) SiteDirInfo { get; }

            public (int subCount, int parentCount) FileDirInfo => _fileDirInfo.Value;

            public Document Toc { get; }

            public TocCandidate((int, int) siteDirInfo, Document toc, Document referencedFile)
            {
                SiteDirInfo = siteDirInfo;
                Toc = toc;
                _fileDirInfo = new Lazy<(int, int)>(() => GetRelativeDirectoryInfo(referencedFile.FilePath.Path, Toc.FilePath.Path));
            }
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, possitive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest(based on site path)
        /// 2. parent nearest(based on site path)
        /// 3. sub nearest(based on file path)
        /// 4. parent nearest(based on file path)
        /// </summary>
        private int CompareTocCandidate(TocCandidate candidateX, TocCandidate candidateY)
        {
            var result = candidateX.SiteDirInfo.subCount - candidateY.SiteDirInfo.subCount;
            if (result == 0)
                result = candidateX.SiteDirInfo.parentCount - candidateY.SiteDirInfo.parentCount;
            if (result == 0)
                result = candidateX.FileDirInfo.subCount - candidateY.FileDirInfo.subCount;
            if (result == 0)
                result = candidateX.FileDirInfo.parentCount - candidateY.FileDirInfo.parentCount;
            if (result == 0)
                result = candidateX.Toc.FilePath.CompareTo(candidateY.Toc.FilePath);
            return result;
        }
    }
}
