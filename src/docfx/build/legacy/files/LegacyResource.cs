// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyResource
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestOutput legacyManifestOutput)
        {
            var metadata = Metadata.GetFromConfig(doc);
            metadata = LegacyMetadata.GenerataCommonMetadata(metadata, docset);
            metadata.Remove("__global");

            context.Move(doc.OutputPath, legacyManifestOutput.ResourceOutput.ToLegacyOutputPath(docset));
            context.WriteJson(metadata, legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset));
        }
    }
}
