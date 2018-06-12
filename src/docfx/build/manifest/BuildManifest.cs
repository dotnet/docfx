// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildManifest
    {
        public static void Build(Context context, List<Document> publishedFiles, DependencyMap sourceDependencies)
        {
            var manifest = new Manifest
            {
                Publish = publishedFiles.Select(ToPublishManifest).ToArray(),
                Dependencies = sourceDependencies?.Where(d => d.Value.Any()).Select(ToDependencyManifest).ToArray(),
            };

            context.WriteJson(manifest, "build.manifest");
        }

        private static PublishManifest ToPublishManifest(Document doc)
        {
            return new PublishManifest
            {
                SiteUrl = doc.SiteUrl,
                OutputPath = doc.OutputPath,
            };
        }

        private static DependencyManifest ToDependencyManifest(KeyValuePair<Document, List<DependencyItem>> dependency)
        {
            return new DependencyManifest
            {
                Source = dependency.Key.FilePath,
                Dependencies = dependency.Value.Select(v => new DependencyManifestItem { Source = v.Dest.FilePath, Type = v.Type }).ToArray(),
            };
        }
    }
}
