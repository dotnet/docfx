// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Legacy
    {
        public static void ConvertToLegacyModel(
            Docset docset,
            Context context,
            FileManifest[] fileManifests,
            DependencyMap dependencyMap,
            TableOfContentsMap tocMap,
            XrefMap xrefMap)
        {
            using (Progress.Start("Converting to legacy"))
            {
                // generate manifest and corresponding files
                var legacyManifestItems = LegacyManifest.Convert(docset, context, fileManifests);
                LegacyOutput.Convert(docset, context, legacyManifestItems);

                // generate mappings
                var files = fileManifests.Select(item => item.File).ToList();
                LegacyFileMap.Convert(docset, context, files);
                LegacyDependencyMap.Convert(docset, context, files, dependencyMap, tocMap);
                LegacyCrossRepoReferenceInfo.Convert(docset, context);
                LegacyXrefMap.Convert(docset, context, xrefMap);
            }
        }
    }
}
