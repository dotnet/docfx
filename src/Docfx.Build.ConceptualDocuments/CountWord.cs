// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Build.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using HtmlAgilityPack;

namespace Docfx.Build.ConceptualDocuments;

[Export(nameof(ConceptualDocumentProcessor), typeof(IDocumentBuildStep))]
public class CountWord : BaseDocumentBuildStep
{
    public override string Name => nameof(CountWord);

    public override int BuildOrder => 1;

    public override void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
        foreach (var model in models)
        {
            if (model.Type == DocumentType.Article)
            {
                var content = (Dictionary<string, object>)model.Content;
                content["wordCount"] = WordCounter.CountWord((string)content[Constants.PropertyName.Conceptual]);
            }
        }
    }
}

internal static class WordCounter
{
    private static readonly string[] ExcludeNodeXPaths = { "//title" };

    public static long CountWord(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        HtmlDocument document = new();

        // Append a space before each end bracket so that InnerText inside different child nodes can separate itself from each other.
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
        char[] delimiterChars = { ' ', '\t', '\n' };

        string[] wordList = text.Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
        return wordList.Count(s => !s.Trim().All(specialChars.Contains));
    }
}
