// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(
            Docset docset,
            Context context,
            List<Document> documents,
            DependencyMap dependencyMap,
            TableOfContentsMap tocMap,
            Report report)
        {
            using (Progress.Start("Converting to legacy"))
            {
                // generate manifest and corresponding files
                var legacyManifestItems = LegacyManifest.Convert(docset, context, documents);
                LegacyOutput.Convert(docset, context, legacyManifestItems, tocMap);

                // generate mappings
                LegacyFileMap.Convert(docset, context, documents);
                LegacyDependencyMap.Convert(docset, context, documents, dependencyMap, tocMap);
                LegacyCrossRepoReferenceInfo.Convert(docset, context);
                LegacyReport.Convert(docset, report);
                LegacyXrefMap.Convert(docset, context);
            }
        }
    }
}
