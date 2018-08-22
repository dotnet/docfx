// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json.Linq;

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

            var outputRootRelativePath =
                PathUtility.NormalizeFolder(
                    Path.GetRelativePath(
                        PathUtility.NormalizeFolder(Path.GetDirectoryName(legacyManifestOutput.PageOutput.OutputPathRelativeToSiteBasePath)),
                        PathUtility.NormalizeFolder(".")));

            var themesRelativePathToOutputRoot = "_themes/";

            JObject rawMetadata;
            if (!string.IsNullOrEmpty(doc.RedirectionUrl))
            {
                rawMetadata = LegacyMetadata.GenerateLegacyRedirectionRawMetadata(docset, pageModel);
                context.WriteJson(new { outputRootRelativePath, content, rawMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
            }
            else
            {
                rawMetadata = LegacyMetadata.GenerateLegacyRawMetadata(pageModel, content, docset, doc, legacyManifestOutput, tocMap);
                var pageMetadata = LegacyMetadata.GenerateLegacyPageMetadata(rawMetadata);
                context.WriteJson(new { outputRootRelativePath, content, rawMetadata, pageMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
            }

            var metadate = LegacyMetadata.GenerateLegacyMetadateOutput(rawMetadata);
            context.WriteJson(metadate, metadataOutputPath);
        }
    }
}
