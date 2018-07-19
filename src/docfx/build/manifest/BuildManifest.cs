// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildManifest
    {
        public static void Build(
            Context context,
            List<Document> files,
            DependencyMap dependencies,
            ContributionInfo contribution,
            CommandLineOptions options)
        {
            var manifest = new Manifest
            {
                Files = files.Select(ToPublishManifest).ToArray(),

                Dependencies = dependencies.ToDictionary(
                    d => d.Key.FilePath,
                    d => d.Value.Select(v =>
                    new DependencyManifestItem
                    {
                        Source = v.Dest.FilePath,
                        Type = v.Type,
                    }).ToArray()),
            };

            context.WriteJson(manifest, "build.manifest");

            FileManifest ToPublishManifest(Document doc)
            {
                var noOutput = doc.ContentType == ContentType.Resource && !doc.Docset.Config.Output.CopyResources;
                var (repo, _, isDocsetRepo) = contribution.GetRepository(doc);
                var overrideRepo = isDocsetRepo ? options.Repo : null;
                var overrideBranch = isDocsetRepo ? options.Branch : null;

                return new FileManifest
                {
                    SourcePath = doc.FilePath,
                    SiteUrl = doc.SiteUrl,
                    OutputPath = noOutput ? null : doc.OutputPath,
                    Repo = overrideRepo ?? repo?.Name,
                    Branch = overrideBranch ?? repo?.Branch,
                    Commit = repo?.Commit,
                };
            }
        }
    }
}
