// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal static class LegacyResource
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestItem legacyManifestItem)
        {
            var legacyManifestOutput = legacyManifestItem.Output;
            var metadata = docset.Metadata.GetMetadata(doc);
            metadata = LegacyMetadata.GenerataCommonMetadata(metadata, docset);
            metadata.Remove("__global");

            if (docset.Config.Output.CopyResources)
            {
                LegacyUtility.MoveFileSafe(
                    docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath),
                    docset.GetAbsoluteOutputPathFromRelativePath(legacyManifestOutput.ResourceOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group)));
            }

            context.WriteJson(metadata, legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset, legacyManifestItem.Group));
        }
    }
}
