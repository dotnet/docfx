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

        private readonly IReadOnlyDictionary<Document, List<Document>> _tocReferences;

        public IEnumerable<FilePath> Files => _tocs.Concat(_experimentalTocs).Where(ShouldBuildFile).Select(toc => toc.FilePath);

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
            return FindNearestToc(file, _tocs, _documentToTocs, file => file.FilePath.Path);
        }

        public static TableOfContentsMap Create(Context context)
        {
            using (Progress.Start("Loading TOC"))
            {
                var builder = new TableOfContentsMapBuilder();
                ParallelUtility.ForEach(
                    context.BuildScope.GetFiles(ContentType.TableOfContents),
                    file => BuildTocMap(context, file, builder),
                    Progress.Update);

                return builder.Build();
            }
        }

        /// <summary>
        /// Compare two toc candidate relative to target file.
        /// Return negative if x is closer than y, positive if x is farer than y, 0 if x equals y.
        /// 1. sub nearest(based on file path)
        /// 2. parent nearest(based on file path)
        /// 3. sub-name lexicographical nearest
        /// </summary>
        internal static T? FindNearestToc<T>(T file, HashSet<T> tocs, IReadOnlyDictionary<T, HashSet<T>> documentsToTocs, Func<T, string> getPath) where T : class, IComparable<T>
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
            if (_tocReferences.TryGetValue(file, out var tocReferences) && tocReferences.Any(toc => toc.FilePath.Origin != FileOrigin.Fallback))
            {
                return true;
            }

            return false;
        }

        private static void BuildTocMap(Context context, FilePath path, TableOfContentsMapBuilder tocMapBuilder)
        {
            try
            {
                var file = context.DocumentProvider.GetDocument(path);
                if (file.ContentType != ContentType.TableOfContents)
                {
                    return;
                }

                var (errors, _, referencedDocuments, referencedTocs) = context.TableOfContentsLoader.Load(file);
                context.ErrorLog.Write(errors);

                tocMapBuilder.Add(file, referencedDocuments, referencedTocs);
            }
            catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
            {
                context.ErrorLog.Write(dex);
            }
        }
    }
}
