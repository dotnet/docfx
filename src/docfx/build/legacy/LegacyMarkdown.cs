// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMarkdown
    {
        public static void Convert(
            Docset docset,
            Context context,
            string absoluteOutputFilePath,
            string relativeOutputFilePath,
            string legacyOutputFilePathRelativeToSiteBasePath)
        {
            var rawPageOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".raw.page.json");
            var metaOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".mta.json");

            File.Move(absoluteOutputFilePath, rawPageOutputPath);

            var pageModel = JsonUtility.Deserialize<PageModel>(File.ReadAllText(rawPageOutputPath));

            var legacyRawMetadata = new LegacyRawMetadata();
            if (!string.IsNullOrEmpty(pageModel.Content))
            {
                legacyRawMetadata.Content = PostProcessHtml(pageModel.Content, docset.Config.Locale);
            }

            legacyRawMetadata.RawMetadata = pageModel.Metadata;
            legacyRawMetadata.RawMetadata.Metadata["toc_rel"] = pageModel.TocRelativePath;
            legacyRawMetadata.RawMetadata.Metadata["locale"] = pageModel.Locale;
            legacyRawMetadata.RawMetadata.Metadata["word_count"] = pageModel.WordCount;
            legacyRawMetadata.RawMetadata.Metadata["_op_rawTitle"] = $"<h1>{pageModel.Metadata.Title}</h1>";
            context.WriteJson(legacyRawMetadata, rawPageOutputPath);
        }

        private static string PostProcessHtml(string content, string locale)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            HtmlUtility.AddLinkType(doc.DocumentNode, locale);
            HtmlUtility.RemoveRerunCodepenIframes(doc.DocumentNode);
            return doc.DocumentNode.OuterHtml;
        }
    }
}
