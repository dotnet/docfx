// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static async Task ConvertToLegacyModel(
            Docset docset,
            Context context,
            List<Document> documents,
            DependencyMap dependencyMap,
            TableOfContentsMap tocMap)
        {
            using (Progress.Start("Converting to legacy"))
            {
                Jint.Init(docset);

                // generate manifest and corresponding files
                var legacyManifestItems = await LegacyManifest.Convert(docset, context, documents);
                await LegacyOutput.Convert(docset, context, legacyManifestItems, tocMap);

                // generate mappings
                await LegacyFileMap.Convert(docset, context, documents);
                await LegacyDependencyMap.Convert(docset, context, documents, dependencyMap, tocMap);
                LegacyCrossRepoReferenceInfo.Convert(docset, context);
                LegacyXrefMap.Convert(docset, context);
            }
        }
    }
}
