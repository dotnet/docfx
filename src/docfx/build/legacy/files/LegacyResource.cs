// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyResource
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestItem legacyManifestItem,
            List<string> monikers)
        {
            var legacyManifestOutput = legacyManifestItem.Output;
            var metadata = new JObject { ["locale"] = docset.Locale };
            if (monikers?.Count > 0)
            {
                metadata["monikers"] = new JArray(monikers);
            }

            if (docset.Config.Output.CopyResources)
            {
                LegacyUtility.MoveFileSafe(
                    docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath),
                    docset.GetAbsoluteOutputPathFromRelativePath(legacyManifestOutput.ResourceOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group)));
            }

            context.Output.WriteJson(metadata, legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group));
        }
    }
}
