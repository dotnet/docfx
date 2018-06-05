// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ResolveHtmlLinks
    {
        public static MarkdownPipelineBuilder UseResolveHtmlLinks(this MarkdownPipelineBuilder builder, MarkdownContext context)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (node is HtmlBlock block)
                    {
                        block.Lines = new StringLineGroup(ResolveLinks(block.Lines.ToString()));
                    }
                    else if (node is HtmlInline inline)
                    {
                        inline.Tag = ResolveLinks(inline.Tag);
                    }
                    return false;
                });
            });

            string ResolveLinks(string html)
            {
                return HtmlUtility.TransformHtml(
                    html,
                    node => node.TransformLink(href => context.GetLink(href, InclusionContext.File)));
            }
        }
    }
}
