// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Markdig;
using Markdig.Helpers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ResolveXrefExtension
    {
        public static MarkdownPipelineBuilder UseResolveXref(
            this MarkdownPipelineBuilder builder,
            Func<string, bool, MarkdownObject, (string href, string display)> resolveXref)
        {
            return builder.Use(document =>
            {
                document.Replace(node =>
                {
                    var file = ((Document)InclusionContext.File).FilePath;
                    if (node is XrefInline xref)
                    {
                        var isShorthand = xref.GetAttributes().Properties.Any(p => p.Key == "data-raw-source" && p.Value.StartsWith("@"));
                        var (href, display) = resolveXref(xref.Href, isShorthand, xref);
                        if (href is null)
                        {
                            return node;
                        }

                        return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                    }
                    else if (node is HtmlBlock block)
                    {
                        block.Lines = new StringLineGroup(ResolveXref(block.Lines.ToString(), block.ToSourceInfo().Line, file, block));
                    }
                    else if (node is HtmlInline inline)
                    {
                        inline.Tag = ResolveXref(inline.Tag, inline.ToSourceInfo().Line, file, inline);
                    }
                    return node;
                });
            });

            string ResolveXref(string html, int startLine, string file, MarkdownObject block)
            {
                return HtmlUtility.TransformXref(
                    html,
                    (href, isShorthand) => resolveXref(href, isShorthand, block));
            }
        }
    }
}
