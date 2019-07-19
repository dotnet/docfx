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
            DependencyMap dependencyMap)
        {
            using (Progress.Start("Converting to legacy"))
            {
                fileManifests = fileManifests.Where(f => !f.Value.HasError).ToDictionary(k => k.Key, v => v.Value);
                var files = fileManifests.Keys.ToList();
                var legacyVersionProvider = new LegacyVersionProvider(docset);

                LegacyManifest.Convert(docset, context, fileManifests);
                var legacyDependencyMap = LegacyDependencyMap.Convert(docset, context, files, dependencyMap, legacyVersionProvider);
                LegacyFileMap.Convert(docset, context, legacyDependencyMap, fileManifests, legacyVersionProvider);
            }
        }
    }
}
