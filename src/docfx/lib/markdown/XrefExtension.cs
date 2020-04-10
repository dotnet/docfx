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
    internal static class XrefExtension
    {
        public static MarkdownPipelineBuilder UseXref(
            this MarkdownPipelineBuilder builder,
            Func<SourceInfo<string>?, SourceInfo<string>?, bool, (string? href, string display)> resolveXref)
        {
            return builder.Use(document =>
            {
                document.Replace(node =>
                {
                    // <xref:uid>
                    // @uid
                    if (node is XrefInline xref)
                    {
                        var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                        var isShorthand = raw.StartsWith("@");
                        var source = new SourceInfo<string>(xref.Href, xref.ToSourceInfo());
                        var (href, display) = resolveXref(source, null, isShorthand);

                        if (href is null)
                        {
                            return new LiteralInline(raw);
                        }

                        return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                    }
                    else if (node is HtmlBlock block)
                    {
                        // inside html block
                        // <p> <xref href="" uid=""/> </p>
                        block.Lines = new StringLineGroup(ResolveXref(block.Lines.ToString(), block));
                    }
                    else if (node is HtmlInline inline)
                    {
                        // inside html inline
                        // text <xref href="" uid=""/> text
                        inline.Tag = ResolveXref(inline.Tag, inline);
                    }
                    return node;
                });
            });

            string ResolveXref(string html, MarkdownObject block)
                => HtmlUtility.TransformXref(html, block, resolveXref);
        }
    }
}
