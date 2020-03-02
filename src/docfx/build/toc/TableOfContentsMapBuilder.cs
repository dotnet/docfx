// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Toc map builder which to build the referenced toc files and mappings between toc and documents
    /// </summary>
    internal class TableOfContentsMapBuilder
    {
        /// <summary>
        /// Mappings between toc and a collection of document
        /// </summary>
        private readonly DictionaryBuilder<Document, (List<Document> files, List<Document> tocs)> _tocReferences
                   = new DictionaryBuilder<Document, (List<Document> files, List<Document> tocs)>();

        /// <summary>
        /// Add toc files and toc mappings
        /// </summary>
        /// <param name="tocFile">The toc file being built</param>
        /// <param name="referencedDocuments">The document files which are referenced by the toc file being built</param>
        /// <param name="referencedTocs">The toc files which are referenced by the toc file being built</param>
        public void Add(Document tocFile, List<Document> referencedDocuments, List<Document> referencedTocs)
        {
            _tocReferences.TryAdd(tocFile, (referencedDocuments, referencedTocs));
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
            var tocReferences = _tocReferences.ToDictionary();
            var includedTocs = tocReferences.Values.SelectMany(item => item.tocs).ToHashSet();

            foreach (var (toc, (documents, _)) in _tocReferences.ToDictionary())
            {
                if (includedTocs.Contains(toc))
                {
                    // TOC been included by other TOCs will be ignored
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

            return new TableOfContentsMap(
                allTocs,
                experimentalTocs,
                documentToTocs,
                tocReferences.ToDictionary(k => k.Key, v => v.Value.tocs));
        }
    }
}
