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
            Dictionary<Document, PublishItem> fileManifests,
            DependencyMap dependencyMap,
            TableOfContentsMap tocMap)
        {
            using (Progress.Start("Converting to legacy"))
            {
                var files = fileManifests.Keys.ToList();

                LegacyManifest.Convert(docset, context, fileManifests);
                var legacyDependencyMap = LegacyDependencyMap.Convert(docset, context, files, dependencyMap, tocMap);
                LegacyFileMap.Convert(docset, context, files, legacyDependencyMap, fileManifests);
            }
        }
    }
}
