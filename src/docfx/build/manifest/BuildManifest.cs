// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildManifest
    {
        public static void Build(Context context, Dictionary<Document, IEnumerable<DependencyItem>> documentWithDependencies)
        {
            if (documentWithDependencies == null || !documentWithDependencies.Any())
            {
                return;
            }

            var manifest = new Manifest
            {
                Files = documentWithDependencies.Keys.Select(ToManifestFile).ToArray(),
                Dependencies = documentWithDependencies.Where(d => d.Value.Any()).Select(ToManifestDependency).ToArray(),
            };

            context.WriteJson(manifest, "build.manifest");
        }

        private static ManifestFile ToManifestFile(Document doc)
        {
            return new ManifestFile
            {
                SiteUrl = doc.SiteUrl,
                OutputPath = doc.OutputPath,
            };
        }

        private static ManifestDependency ToManifestDependency(KeyValuePair<Document, IEnumerable<DependencyItem>> dependency)
        {
            return new ManifestDependency
            {
                Source = dependency.Key.FilePath,
                Dependencies = dependency.Value.Select(v => new ManifestDependencyItem { Source = v.Document.FilePath, Type = v.Type.ToString() }).ToArray(),
            };
        }
    }
}
