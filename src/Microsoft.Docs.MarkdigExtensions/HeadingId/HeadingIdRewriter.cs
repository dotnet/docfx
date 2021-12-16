// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.MarkdigExtensions;

public class HeadingIdRewriter : IMarkdownObjectRewriter
{
    private static readonly Regex s_openAnchorRegex = new(@"^\<a +(?:name|id)=\""([\w \-\.]+)\"" *\>$", RegexOptions.Compiled);
    private static readonly Regex s_closeAnchorRegex = new(@"^\<\/a\>$", RegexOptions.Compiled);

    public void PostProcess(IMarkdownObject markdownObject)
    {
    }

    public void PreProcess(IMarkdownObject markdownObject)
    {
    }

    public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
    {
        if (markdownObject is HeadingBlock block)
        {
            if (block.Inline.Count() <= 2)
            {
                return block;
            }

            var id = ParseHeading(block);
            if (string.IsNullOrEmpty(id))
            {
                return block;
            }

            var attribute = block.GetAttributes();
            attribute.Id = id;

            return RemoveHtmlTag(block);
        }

        return markdownObject;
    }

    private static HeadingBlock RemoveHtmlTag(HeadingBlock block)
    {
        var inlines = block.Inline.Skip(2);
        block.Inline = new ContainerInline();
        foreach (var inline in inlines)
        {
            inline.Remove();
            block.Inline.AppendChild(inline);
        }
        return block;
    }

    private static string ParseHeading(HeadingBlock headBlock)
    {
        var tokens = headBlock.Inline.ToList();
        if (tokens[0] is not HtmlInline openAnchorTag || tokens[1] is not HtmlInline closeAnchorTag)
        {
            return null;
        }

        var m = s_openAnchorRegex.Match(openAnchorTag.Tag);
        if (!m.Success)
        {
            return null;
        }
        if (!s_closeAnchorRegex.IsMatch(closeAnchorTag.Tag))
        {
            return null;
        }

        return m.Groups[1].Value;
    }
}
