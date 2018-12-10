// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(
            Docset docset,
            Context context,
            Dictionary<Document, FileManifest> fileManifests,
            DependencyMap dependencyMap,
            TableOfContentsMap tocMap,
            MetadataProvider metadataProvider,
            MonikersProvider monikersProvider)
        {
            using (Progress.Start("Converting to legacy"))
            {
                // generate manifest and corresponding files
                var legacyManifestItems = LegacyManifest.Convert(docset, context, fileManifests, monikersProvider);
                LegacyOutput.Convert(docset, context, metadataProvider, legacyManifestItems);

                // generate mappings
                var files = fileManifests.Keys.ToList();
                LegacyFileMap.Convert(docset, context, files);
                LegacyDependencyMap.Convert(docset, context, files, dependencyMap, tocMap);
                LegacyCrossRepoReferenceInfo.Convert(docset, context);
            }
        }
    }
}
