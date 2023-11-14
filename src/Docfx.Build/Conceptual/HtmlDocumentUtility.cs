// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using HtmlAgilityPack;

namespace Docfx.Build.ConceptualDocuments;

static class HtmlDocumentUtility
{
    public static SeparatedHtmlInfo SeparateHtml(string contentHtml)
    {
        ArgumentNullException.ThrowIfNull(contentHtml);

        var content = new SeparatedHtmlInfo();

        var document = new HtmlDocument();
        document.LoadHtml(contentHtml);

        // TODO: how to get TITLE
        // InnerText in HtmlAgilityPack is not decoded, should be a bug
        var headerNode = document.DocumentNode.SelectSingleNode("//h1|//h2|//h3");
        content.Title = WebUtility.HtmlDecode(headerNode?.InnerText);

        if (headerNode != null && GetFirstNoneCommentChild(document.DocumentNode) == headerNode)
        {
            content.RawTitle = headerNode.OuterHtml;
            headerNode.Remove();
        }
        else
        {
            content.RawTitle = string.Empty;
        }

        content.Content = document.DocumentNode.OuterHtml;

        return content;
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
