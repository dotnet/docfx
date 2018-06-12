// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildManifest
    {
        public static void Build(Context context, List<Document> files, DependencyMap dependencies)
        {
            var manifest = new Manifest
            {
                Files = files.Select(ToPublishManifest).ToArray(),
                Dependencies = dependencies.Select(ToDependencyManifest).ToArray(),
            };

            context.WriteJson(manifest, "build.manifest");
        }

        private static FileManifest ToPublishManifest(Document doc)
        {
            return new FileManifest
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
