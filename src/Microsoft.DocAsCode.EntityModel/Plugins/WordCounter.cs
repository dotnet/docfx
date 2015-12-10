// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Linq;
    using System.Text;

    using HtmlAgilityPack;

    public static class WordCounter
    {
        private static readonly string[] ExcludeNodeXPaths = { "//title" };

        public static int CountWord(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            StringBuilder htmlString = new StringBuilder();
            htmlString.Append(html);

            // Append a space before each end bracket so that InnerText inside different child nodes can seperate itself from each other.
            htmlString.Replace("</", " </");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlString.ToString());

            int wordCount = 0;
            HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("/");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    wordCount += CountWordInText(node.InnerText);

                    foreach (var excludeNodeXPath in ExcludeNodeXPaths)
                    {
                        HtmlNodeCollection excludeNodes = node.SelectNodes(excludeNodeXPath);
                        if (excludeNodes != null)
                        {
                            foreach (var excludeNode in excludeNodes)
                            {
                                wordCount -= CountWordInText(excludeNode.InnerText);
                            }
                        }
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

            string specialChars = ".?!;:,()[]";
            char[] delimChars = { ' ', '\t', '\n' };

            string[] wordList = text.Split(delimChars, StringSplitOptions.RemoveEmptyEntries);
            return wordList.Count(s => !s.Trim().All(specialChars.Contains));
        }
    }
}
