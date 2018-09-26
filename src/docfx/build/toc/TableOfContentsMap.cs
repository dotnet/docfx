// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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
        /// when the file is not referenced, return only toc in the same folder or parents folder.
        /// i.e. relativePath only contains "..".
        /// </summary>
        public Document GetNearestToc(Document file)
        {
            var nearestToc = (Document)null;

            if (!_documentToTocs.TryGetValue(file, out var referencedTocFiles))
            {
                // fallback to all tocs if no toc files reference this file
                // filter toc files by relative path: subdircount = 0, min parentdir
                // get default nonreferenced toc
                nearestToc = (from toc in _tocs
                              let dirInfo = GetRelativeDirectoryInfo(file, toc)
                              where dirInfo.subDirectoryCount == 0
                              orderby dirInfo.parentDirectoryCount
                              select toc)
                             .FirstOrDefault();
            }
            else
            {
                // from referenced pick the nearest one
                // 1. sub count
                // 2. sub nearest.
                // 3. parent nearest
                // 4. sub-name word-level levenshtein distance nearest
                // 5. sub-name lexicographical nearest
                var tocCandidates = (from toc in referencedTocFiles
                                     let dirInfo = GetRelativeDirectoryInfo(file, toc)
                                     orderby dirInfo.subDirectoryCount,
                                             dirInfo.parentDirectoryCount
                                     select new
                                     {
                                         toc,
                                         dirInfo,
                                     }).Take(5);

                var nearestCandidate = tocCandidates.First();
                var leftCandidates = from tocInfo in tocCandidates
                                     where tocInfo.dirInfo.parentDirectoryCount == nearestCandidate.dirInfo.parentDirectoryCount
                                     && tocInfo.dirInfo.subDirectoryCount == nearestCandidate.dirInfo.subDirectoryCount
                                     select tocInfo;
                nearestToc = leftCandidates.Count() == 1 ? nearestCandidate.toc
                    : (from candidate in leftCandidates
                       orderby Levenshtein.GetLevenshteinDistance(
                           Regex.Split(Path.GetFileNameWithoutExtension(file.SitePath), "[^a-zA-Z0-9]+"),
                           candidate.dirInfo.paths.ToArray(),
                           StringComparer.OrdinalIgnoreCase),
                                 candidate.toc.SitePath
                       select candidate.toc).First();
            }
            return nearestToc;
        }

        private static (int subDirectoryCount, int parentDirectoryCount, IList<string> paths)
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
            var paths = new List<string>();
            foreach (var part in relativePathParts)
            {
                switch (part)
                {
                    case "..":
                        parentDirectoryCount++;
                        break;
                    default:
                        paths.AddRange(Regex.Split(Path.GetFileNameWithoutExtension(part), "[^a-zA-Z0-9]+"));
                        break;
                }
            }
            subDirectoryCount = relativePathParts.Count() - parentDirectoryCount;
            return (subDirectoryCount, parentDirectoryCount, paths);
        }
    }
}
