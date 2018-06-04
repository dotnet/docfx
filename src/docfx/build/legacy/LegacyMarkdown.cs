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
            if (!string.IsNullOrEmpty(pageModel.Content))
            {
                pageModel.Content = PostProcessHtml(pageModel.Content, docset.Config.Locale);
            }

            context.WriteJson(pageModel, rawPageOutputPath);
        }

        private static string PostProcessHtml(string content, string locale)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            HtmlUtility.AddLinkType(doc.DocumentNode, locale);
            return doc.DocumentNode.OuterHtml;
        }
    }
}
