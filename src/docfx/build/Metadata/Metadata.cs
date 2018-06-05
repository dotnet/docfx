// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class Metadata
    {
        private const string SpecialChars = ".?!;:,()[]";
        private static readonly char[] s_delimChars = { ' ', '\t', '\n' };
        private static readonly string[] ExcludeNodeXPaths = { "//title" };

        public static JObject GetFromConfig(Document file)
        {
            Debug.Assert(file != null);

            var config = file.Docset.Config;
            var fileMetadata =
                from item in config.FileMetadata
                where item.Match(file.FilePath)
                select item.Value;

            return JsonUtility.Merge(config.GlobalMetadata, fileMetadata);
        }

        public static JObject GenerateRawMetadata(Context context, Document file, TableOfContentsMap tocMap, string html)
        {
            var rawMetadata = new JObject();
            var locale = file.Docset.Config.Locale;
            var depotName = $"{file.Docset.Config.Product}{file.Docset.Config.DocsetName}";

            rawMetadata["_op_canonicalUrlPrefix"] = $"https://docs.microsoft.com/{file.Docset.Config.Locale}/{file.Docset.Config.SiteBasePath}";
            rawMetadata["_op_pdfUrlPrefixTemplate"] = $"https://docs.microsoft.com/pdfstore/{locale}/{depotName}/{{branchName}}{{pdfName}}";
            rawMetadata["_op_rawTitle"] = GetRawTitle(html);

            rawMetadata["_op_wordCount"] = rawMetadata["word_count"] = CountWord(html);

            rawMetadata["depot_name"] = depotName;
            rawMetadata["is_dynamic_rendering"] = true;
            rawMetadata["layout"] = "Conceptual";
            rawMetadata["locale"] = locale;

            rawMetadata["site_name"] = "Docs";
            rawMetadata["toc_rel"] = tocMap.FindTocRelativePath(file);
            rawMetadata["version"] = 0;

            return rawMetadata;
        }

        internal static long CountWord(string html)
        {
            // TODO: word count does not work for CJK locales...
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            HtmlDocument document = new HtmlDocument();

            // Append a space before each end bracket so that InnerText inside different child nodes can separate itself from each other.
            document.LoadHtml(html.Replace("</", " </", StringComparison.OrdinalIgnoreCase));

            long wordCount = CountWordInText(document.DocumentNode.InnerText);

            foreach (var excludeNodeXPath in ExcludeNodeXPaths)
            {
                HtmlNodeCollection excludeNodes = document.DocumentNode.SelectNodes(excludeNodeXPath);
                if (excludeNodes != null)
                {
                    foreach (var excludeNode in excludeNodes)
                    {
                        wordCount -= CountWordInText(excludeNode.InnerText);
                    }
                }
            }

            return wordCount;
        }

        private static int CountWordInText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            string[] wordList = text.Split(s_delimChars, StringSplitOptions.RemoveEmptyEntries);
            return wordList.Count(s => !s.Trim().All(SpecialChars.Contains));
        }

        private static string GetRawTitle(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            var headerNode = document.DocumentNode.SelectSingleNode("//h1|//h2|//h3");

            var rawTitle = "";

            if (headerNode != null && GetFirstNoneCommentChild(document.DocumentNode) == headerNode)
            {
                rawTitle = headerNode.OuterHtml;
            }

            return rawTitle;
        }

        private static HtmlNode GetFirstNoneCommentChild(HtmlNode node)
        {
            var result = node.FirstChild;
            while (result != null && (result.NodeType == HtmlNodeType.Comment || string.IsNullOrWhiteSpace(result.OuterHtml)))
            {
                result = result.NextSibling;
            }
            return result;
        }
    }
}
