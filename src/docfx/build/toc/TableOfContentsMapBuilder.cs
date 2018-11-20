// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Toc map builder which to build the referenced toc files and mappings between toc and documents
    /// </summary>
    internal class TableOfContentsMapBuilder
    {
        /// <summary>
        /// Tracks toc files which are included by other toc files
        /// Included toc files are excluded when finding nearest toc for an article.
        /// </summary>
        private readonly ConcurrentHashSet<Document> _referencedTocs = new ConcurrentHashSet<Document>();

        /// <summary>
        /// Mappings between toc and a collection of document
        /// </summary>
        private readonly ConcurrentDictionary<Document, IEnumerable<Document>> _tocToDocuments = new ConcurrentDictionary<Document, IEnumerable<Document>>();

        /// <summary>
        /// Add toc files and toc mappings
        /// </summary>
        /// <param name="tocFile">The toc file being built</param>
        /// <param name="referencedDocuments">The document files which are referenced by the toc file being built</param>
        /// <param name="referencedTocs">The toc files which are referenced by the toc file being built</param>
        public void Add(Document tocFile, IEnumerable<Document> referencedDocuments, IEnumerable<Document> referencedTocs)
        {
            _tocToDocuments.TryAdd(tocFile, referencedDocuments);
            foreach (var referencedToc in referencedTocs)
            {
                _referencedTocs.TryAdd(referencedToc);
            }
        }

        /// <summary>
        /// Build toc map including all tocs and reversed toc mapping(document -> toc)
        /// </summary>
        /// <returns>The toc map</returns>
        public TableOfContentsMap Build(Context context)
        {
            var documentToTocs = new Dictionary<Document, HashSet<Document>>();

            // reverse the mapping between toc and documents
            // order by toc path
            var allTocs = new List<Document>();
            var experimentalTocs = new List<Document>();

            // handle conflicts
            var tocsGroupBySiteUrl = _tocToDocuments.Keys.GroupBy(k => k.SiteUrl);
            var conflictedTocs = new HashSet<Document>();
            foreach (var group in tocsGroupBySiteUrl)
            {
                var siteUrl = group.Key;
                if (group.Count() > 1)
                {
                    context.Report(Errors.PublishUrlConflict(siteUrl, group));
                    foreach (var toc in group)
                    {
                        conflictedTocs.Add(toc);
                    }
                }
            }

            foreach (var (toc, documents) in _tocToDocuments)
            {
                if (_referencedTocs.Contains(toc))
                {
                    // referenced toc's mapping will be ignored
                    continue;
                }

                if (conflictedTocs.Contains(toc))
                {
                    // conflicted tocs will be removed from toc map
                    continue;
                }

                if (toc.IsExperimental)
                {
                    // experimental toc will be ignored
                    experimentalTocs.Add(toc);
                    continue;
                }

                allTocs.Add(toc);
                foreach (var document in documents)
                {
                    if (!documentToTocs.TryGetValue(document, out var tocs))
                    {
                        documentToTocs[document] = tocs = new HashSet<Document>();
                    }

                    tocs.Add(toc);
                }
            }

            return new TableOfContentsMap(allTocs, experimentalTocs, documentToTocs);
        }
    }
}
