// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildManifest
    {
        public static void Build(Context context, List<Document> documents)
        {
            if (documents.Count <= 0)
                return;

            var manifest = new Manifest
            {
                Files = documents.Select(ToManifestFile).ToArray(),
            };

            context.WriteJson(manifest, "build.manifest");
        }

        private static ManifestFile ToManifestFile(Document doc)
        {
            return new ManifestFile
            {
                Url = doc.SiteUrl,
                Path = doc.OutputPath,
            };
        }
    }
}
