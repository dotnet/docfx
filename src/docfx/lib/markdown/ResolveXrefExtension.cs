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
                     var file = (Document)InclusionContext.File;
                     if (node is XrefInline xref)
                     {
                         var (_, href, display, _) = resolveXref(xref.Href, xref);
                         if (href is null)
                         {
                             var raw = new SourceInfo<string>(xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value, node.ToSourceInfo());
                             var error = raw.Value.StartsWith("@")
                                 ? Errors.AtUidNotFound(file, xref.Href, raw)
                                 : Errors.UidNotFound(file, xref.Href, raw);

                             MarkdownUtility.LogError(error);
                             return new LiteralInline(raw);
                         }
                         return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                     }
                     if (node is HtmlBlock block)
                     {
                         var (errors, result) = ResolveXrefs(block.Lines.ToString(), (Document)InclusionContext.File, block);
                         foreach (var (uid, line, column) in errors)
                         {
                             MarkdownUtility.LogError(Errors.UidNotFound((Document)InclusionContext.File, uid, new SourceInfo<string>(block.Lines.ToString(), new SourceInfo(file.FilePath, line, column))));
                         }
                         block.Lines = new StringLineGroup(result);
                     }
                     else if (node is HtmlInline inline)
                     {
                         var (errors, result) = ResolveXrefs(inline.Tag, (Document)InclusionContext.File, inline);
                         foreach (var (uid, line, column) in errors)
                         {
                             MarkdownUtility.LogError(Errors.UidNotFound((Document)InclusionContext.File, uid, new SourceInfo<string>(inline.Tag, new SourceInfo(file.FilePath, line, column))));
                         }
                         inline.Tag = result;
                     }
                     return node;
                 });
             });

            (List<(string uid, int line, int column)>, string) ResolveXrefs(string html, Document file, MarkdownObject block)
                => HtmlUtility.TransformXrefs(html, href => MarkdownUtility.ResolveXref(href, block));
        }
    }
}
