// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// The mappings between toc and document
    /// </summary>
    internal class TableOfContentsMap
    {
        private readonly List<Document> _tocs;

        private readonly Dictionary<Document, HashSet<Document>> _documentToTocs;

        public TableOfContentsMap(List<Document> tocs, Dictionary<Document, HashSet<Document>> documentToTocs)
        {
            _tocs = tocs ?? throw new ArgumentNullException(nameof(tocs));
            _documentToTocs = documentToTocs ?? throw new ArgumentNullException(nameof(documentToTocs));
        }

        /// <summary>
        /// Find the toc relative path to document
        /// </summary>
        /// <param name="file">Document</param>
        /// <returns>The toc relative path</returns>
        public string FindTocRelativePath(Document file)
        {
            var nearestToc = GetNearestToc(file);

            return nearestToc != null ? PathUtility.NormalizeFile(PathUtility.GetRelativePathToFile(file.OutputPath, nearestToc.OutputPath)) : null;
        }

        /// <summary>
        /// Return the nearest toc relative to the current file
        /// "near" means less subdirectory count
        /// when subdirectory counts are same, "near" means less parent directory count
        /// e.g. "../../a/TOC.md" is nearer than "b/c/TOC.md"
        /// </summary>
        private Document GetNearestToc(Document file)
        {
            // fallback to all tocs if no toc files reference this file
            var filteredTocFiles = _documentToTocs.TryGetValue(file, out var referencedTocFiles) ? (IEnumerable<Document>)referencedTocFiles : _tocs;

            var nearstToc = (Document)null;
            var nearestSubDirCount = 0;
            var nearestParentDirCount = 0;
            foreach (var toc in filteredTocFiles)
            {
                var relativePath = PathUtility.GetRelativePathToFile(toc.FilePath, file.FilePath);
                var (subDirCount, parentDirCount) = GetDirectoryCount(relativePath);
                var distance = Compare(nearestSubDirCount, nearestParentDirCount, subDirCount, parentDirCount);
                if (nearstToc == null || distance > 0 ||
                    (distance == 0 && string.Compare(nearstToc.FilePath, toc.FilePath, StringComparison.OrdinalIgnoreCase) > 0))
                {
                    nearstToc = toc;
                    nearestSubDirCount = subDirCount;
                    nearestParentDirCount = parentDirCount;
                }
            }

            return nearstToc;
        }

        private static int Compare(int xSubDirCount, int xParentDirCount, int ySubDirCount, int yParentDirCount)
        {
            if (xSubDirCount == ySubDirCount)
            {
                return xParentDirCount - yParentDirCount;
            }
            else
            {
                return xSubDirCount - ySubDirCount;
            }
        }

        private static (int subDirectoryCount, int parentDirectoryCount) GetDirectoryCount(string relativePath)
        {
            relativePath = PathUtility.NormalizeFile(relativePath);

            // todo: perf optimization, don't split '/' here again.
            var relativePathParts = relativePath.Split('/');
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

            subDirectoryCount = relativePathParts.Length - parentDirectoryCount;
            return (subDirectoryCount, parentDirectoryCount);
        }
    }
}
