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
            LegacyManifestOutput legacyManifestOutput,
            TableOfContentsMap tocMap)
        {
            var rawPageOutputPath = legacyManifestOutput.PageOutput.ToLegacyOutputPath(docset);
            var metadataOutputPath = legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset);
            LegacyUtility.MoveFileSafe(
                docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath),
                docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath));

            var (_, pageModel) = JsonUtility.Deserialize<PageModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath)));

            var content = pageModel.Content as string;
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

            var rawMetadata = LegacyMetadata.GenerateLegacyRawMetadata(pageModel, content, docset, doc, legacyManifestOutput, tocMap);

            rawMetadata = Jint.Run(rawMetadata);
            var pageMetadata = LegacyMetadata.GenerateLegacyPageMetadata(rawMetadata);

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
