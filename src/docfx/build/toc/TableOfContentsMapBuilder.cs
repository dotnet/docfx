// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;

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
        private readonly ConcurrentDictionary<Document, byte> _referencedTocs = new ConcurrentDictionary<Document, byte>();

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
                _referencedTocs.TryAdd(referencedToc, 0);
            }
        }

        /// <summary>
        /// Build toc map including all tocs and reversed toc mapping(document -> toc)
        /// </summary>
        /// <returns>The toc map</returns>
        public TableOfContentsMap Build()
        {
            var documentToTocs = new Dictionary<Document, HashSet<Document>>();

            // reverse the mapping between toc and documents
            // order by toc path
            var allTocs = new List<Document>();
            foreach (var (toc, documents) in _tocToDocuments)
            {
                if (_referencedTocs.ContainsKey(toc))
                {
                    // referenced toc's mapping will be ignored
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

            return new TableOfContentsMap(allTocs, documentToTocs);
        }
    }
}
