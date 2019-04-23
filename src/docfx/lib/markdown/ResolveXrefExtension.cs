// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            Func<string, MarkdownObject, (Error error, string href, string display, Document file)> resolveXref)
        {
            return builder.Use(document =>
             {
                 document.Replace(node =>
                 {
                     var file = ((Document)InclusionContext.File).FilePath;
                     if (node is XrefInline xref)
                     {
                         var (_, href, display, _) = resolveXref(xref.Href, xref);
                         if (href is null)
                         {
                             var raw = new SourceInfo<string>(xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value, node.ToSourceInfo());
                             var error = raw.Value.StartsWith("@")
                                 ? Errors.AtUidNotFound(xref.Href, raw)
                                 : Errors.UidNotFound(xref.Href, raw);

                             MarkdownUtility.LogError(error);
                             return new LiteralInline(raw);
                         }
                         return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                     }
                     else if (node is HtmlBlock block)
                     {
                         var (errors, result) = ResolveXref(block.Lines.ToString(), block.ToSourceInfo().Line, file, block);
                         errors.ForEach(e => MarkdownUtility.LogError(e));
                         block.Lines = new StringLineGroup(result);
                     }
                     else if (node is HtmlInline inline)
                     {
                         var (errors, result) = ResolveXref(inline.Tag, inline.ToSourceInfo().Line, file, inline);
                         errors.ForEach(e => MarkdownUtility.LogError(e));
                         inline.Tag = result;
                     }
                     return node;
                 });
             });

            (List<Error>, string) ResolveXref(string html, int startLine, string file, MarkdownObject block)
                => HtmlUtility.TransformXref(html, startLine, file, href => MarkdownUtility.ResolveXref(href, block));
        }
    }
}
