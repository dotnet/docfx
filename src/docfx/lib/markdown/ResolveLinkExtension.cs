// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ResolveLinkExtension
    {
        public static MarkdownPipelineBuilder UseResolveLink(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (node is LinkInline link)
                    {
                        link.Url = MarkdownUtility.GetLink(link.Url, InclusionContext.File, InclusionContext.RootFile, link) ?? link.Url;
                    }
                    else if (node is HtmlBlock block)
                    {
                        block.Lines = new StringLineGroup(ResolveLinks(block.Lines.ToString(), block));
                    }
                    else if (node is HtmlInline inline)
                    {
                        inline.Tag = ResolveLinks(inline.Tag, inline);
                    }
                    return false;
                });
            });

            string ResolveLinks(string html, MarkdownObject block)
            {
                return HtmlUtility.TransformLinks(
                    html,
                    href => MarkdownUtility.GetLink(href, InclusionContext.File, InclusionContext.RootFile, block));
            }
        }
    }
}
