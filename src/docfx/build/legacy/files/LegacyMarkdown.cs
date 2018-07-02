// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMarkdown
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            GitRepoInfoProvider repo,
            LegacyManifestOutput legacyManifestOutput,
            TableOfContentsMap tocMap)
        {
            var rawPageOutputPath = legacyManifestOutput.PageOutput.ToLegacyOutputPath(docset);
            var metadataOutputPath = legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset);
            File.Delete(rawPageOutputPath.ToAbsoluteOutputPath(docset));
            File.Move(doc.OutputPath.ToAbsoluteOutputPath(docset), rawPageOutputPath.ToAbsoluteOutputPath(docset));

            var pageModel = JsonUtility.Deserialize<PageModel>(File.ReadAllText(rawPageOutputPath.ToAbsoluteOutputPath(docset)));
            var content = pageModel.Content;

            var rawMetadata = LegacyMetadata.GenerateLegacyRawMetadata(pageModel, docset, doc, repo, legacyManifestOutput, tocMap);

            rawMetadata = Jint.Run(rawMetadata);
            var pageMetadata = LegacyMetadata.GenerateLegacyPageMetadata(rawMetadata);

            if (!string.IsNullOrEmpty(content))
            {
                content = HtmlUtility.TransformHtml(
                    content,
                    node => node.AddLinkType(docset.Config.Locale)
                                .RemoveRerunCodepenIframes());
            }
            else
            {
                content = "<div></div>";
            }

            var outputRootRelativePath =
                PathUtility.NormalizeFolder(
                    Path.GetRelativePath(
                        PathUtility.NormalizeFolder(Path.GetDirectoryName(legacyManifestOutput.PageOutput.OutputPathRelativeToSiteBasePath)),
                        PathUtility.NormalizeFolder(".")));

            var themesRelativePathToOutputRoot = "_themes/";

            var metadate = LegacyMetadata.GenerateLegacyMetadateOutput(rawMetadata);

            context.WriteJson(new { outputRootRelativePath, content, rawMetadata, pageMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
            context.WriteJson(metadate, metadataOutputPath);
        }
    }
}
