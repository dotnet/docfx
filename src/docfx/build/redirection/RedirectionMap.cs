// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionMap
    {
        // A -> B
        // C -> B
        public Dictionary<Document, string> CombinedRedirectTo { get; } = new Dictionary<Document, string>();

        // B <- A with document id
        // B <- C with document id
        public Dictionary<Document, List<Document>> RedirectFrom { get; } = new Dictionary<Document, List<Document>>();

        public RedirectionMap(Docset docset, List<Document> files)
        {
            var filesGroupBySiteUrl = files.ToDictionary(f => f.SiteUrl, f => f, StringComparer.OrdinalIgnoreCase);

            // load redirections with document id
            foreach (var (pathToDocset, redirectTo) in docset.Config.Redirections)
            {
                var (document, error) = Document.TryCreate(docset, pathToDocset, true);
                if (error != null)
                {
                    // just throw to abort the whole process
                    throw error;
                }
                CombinedRedirectTo.Add(document, redirectTo);

                if (filesGroupBySiteUrl.TryGetValue(redirectTo, out var redirectToDoc))
                {
                    if (!RedirectFrom.TryGetValue(redirectToDoc, out var redirectFromDocs))
                    {
                        redirectFromDocs = RedirectFrom[redirectToDoc] = new List<Document>();
                    }

                    redirectFromDocs.Add(document);
                }
            }

            // load redirections without document id
            foreach (var (pathToDocset, redirectTo) in docset.Config.RedirectionsWithoutId)
            {
                var (document, error) = Document.TryCreate(docset, pathToDocset, true);
                if (error != null)
                {
                    // just throw to abort the whole process
                    throw error;
                }
                CombinedRedirectTo.Add(document, redirectTo);
            }
        }

        public (DocfxException error, string id, string versionIndependentId) GetIds(Document file)
        {
            var documentId = file.Id.docId;
            var versionId = file.Id.versionIndependentId;

            var error = (DocfxException)null;
            if (RedirectFrom.TryGetValue(file, out var redirectFromDocs))
            {
                if (redirectFromDocs.Count > 1)
                {
                    error = Errors.RedirectionDocumentIdConflict(redirectFromDocs, file);
                }

                var redirectFromDoc = redirectFromDocs.FirstOrDefault();
                if (redirectFromDoc != null)
                {
                    documentId = redirectFromDoc.Id.docId;
                    versionId = redirectFromDoc.Id.versionIndependentId;
                }
            }

            return (error, documentId, versionId);
        }
    }
}
