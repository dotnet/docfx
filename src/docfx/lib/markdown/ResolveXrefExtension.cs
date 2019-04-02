// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Markdig;
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
                     if (node is XrefInline xref)
                     {
                         var (_, href, display, _) = resolveXref(xref.Href, xref);
                         if (href is null)
                         {
                             var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                             var error = raw.StartsWith("@")
                                 ? Errors.AtUidNotFound((Document)InclusionContext.File, xref.Href, raw)
                                 : Errors.UidNotFound((Document)InclusionContext.File, xref.Href, raw);

                             MarkdownUtility.LogError(error);
                             return new LiteralInline(raw);
                         }
                         return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                     }
                     return node;
                 });
             });
        }
    }
}
