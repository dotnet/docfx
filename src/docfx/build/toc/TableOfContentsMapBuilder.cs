// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public void Add(Document tocFile, IEnumerable<Document> references)
        {
            _tocToDocuments.TryAdd(tocFile, references.Where(file => file.ContentType == ContentType.Page));

            foreach (var reference in references)
            {
                if (reference.ContentType == ContentType.TableOfContents)
                {
                    _referencedTocs.TryAdd(reference);
                }
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
            var experimentalTocs = new List<Document>();

            foreach (var (toc, documents) in _tocToDocuments)
            {
                if (_referencedTocs.Contains(toc))
                {
                    // referenced toc's mapping will be ignored
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
