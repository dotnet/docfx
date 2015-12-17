// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;

    using HtmlAgilityPack;

    using Microsoft.DocAsCode.Plugins;

    [Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
    public class CountWord : BaseDocumentBuildStep
    {
        public override string Name => nameof(CountWord);

        public override int BuildOrder => 1;

        public override IEnumerable<FileModel> Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var model in models)
            {
                if (model.Type == DocumentType.Article)
                {
                    var content = (Dictionary<string, object>)model.Content;
                    content["wordCount"] = WordCounter.CountWord((string)content["conceptual"]);
                }
            }
            return models;
        }
    }

    internal static class WordCounter
    {
        private static readonly string[] ExcludeNodeXPaths = { "//title" };

        public static long CountWord(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException(nameof(html));
            }

            HtmlDocument document = new HtmlDocument();

            // Append a space before each end bracket so that InnerText inside different child nodes can seperate itself from each other.
            document.LoadHtml(html.Replace("</", " </"));

            long wordCount = 0;
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
