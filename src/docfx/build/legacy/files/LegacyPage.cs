// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyPage
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document doc,
            LegacyManifestOutput legacyManifestOutput)
        {
            // OPS build use TOC ouput as data page
            if (legacyManifestOutput.TocOutput != null)
            {
                var outputPath = legacyManifestOutput.TocOutput.ToLegacyOutputPath(docset);
                var (_, model) = JsonUtility.Deserialize<PageModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(outputPath)));
                context.Delete(doc.OutputPath);
                context.WriteJson(model.Content, outputPath);
            }

            JObject rawMetadata = null;
            if (legacyManifestOutput.PageOutput != null)
            {
                var rawPageOutputPath = legacyManifestOutput.PageOutput.ToLegacyOutputPath(docset);
                LegacyUtility.MoveFileSafe(
                    docset.GetAbsoluteOutputPathFromRelativePath(doc.OutputPath),
                    docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath));

                var (_, pageModel) = JsonUtility.Deserialize<PageModel>(File.ReadAllText(docset.GetAbsoluteOutputPathFromRelativePath(rawPageOutputPath)));

                var content = pageModel.Content as string;
                if (!string.IsNullOrEmpty(content))
                {
                    content = HtmlUtility.TransformHtml(
                        content,
                        node => node.AddLinkType(docset.Locale, docset.Legacy)
                                    .RemoveRerunCodepenIframes());
                }

                var outputRootRelativePath =
                    PathUtility.NormalizeFolder(
                        Path.GetRelativePath(
                            PathUtility.NormalizeFolder(Path.GetDirectoryName(legacyManifestOutput.PageOutput.RelativePath)),
                            PathUtility.NormalizeFolder(".")));

                var themesRelativePathToOutputRoot = "_themes/";

                if (!string.IsNullOrEmpty(doc.RedirectionUrl))
                {
                    rawMetadata = LegacyMetadata.GenerateLegacyRedirectionRawMetadata(docset, pageModel);
                    context.WriteJson(new { outputRootRelativePath, rawMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
                }
                else
                {
                    rawMetadata = LegacyMetadata.GenerateLegacyRawMetadata(pageModel, content, doc);
                    var pageMetadata = LegacyMetadata.CreateHtmlMetaTags(rawMetadata);
                    context.WriteJson(new { outputRootRelativePath, content, rawMetadata, pageMetadata, themesRelativePathToOutputRoot }, rawPageOutputPath);
                }
            }

            if (legacyManifestOutput.MetadataOutput != null && rawMetadata != null)
            {
                var metadataOutputPath = legacyManifestOutput.MetadataOutput.ToLegacyOutputPath(docset);
                var metadate = LegacyMetadata.GenerateLegacyMetadateOutput(rawMetadata);
                context.WriteJson(metadate, metadataOutputPath);
            }
        }
    }
}
