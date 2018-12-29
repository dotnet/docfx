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
            var metadata = context.MetadataProvider.GetMetadata(doc).metadata;
            metadata = LegacyMetadata.GenerataCommonMetadata(metadata, docset);

            var metadataNeedToBeRemove = new List<string> { "__global" };
            foreach (var property in metadata)
            {
                if (property.Key.StartsWith("_") && !property.Key.StartsWith("_op_"))
                {
                    metadataNeedToBeRemove.AddIfNotNull(property.Key);
                }
            }
            foreach (var key in metadataNeedToBeRemove)
            {
                metadata.Remove(key);
            }
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
