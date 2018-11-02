// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Web;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ResolveXref
    {
        public static MarkdownPipelineBuilder UseResolveXref(this MarkdownPipelineBuilder builder, Func<string, XrefSpec> resolveXref)
        {
            return builder.Use(document =>
             {
                 document.Replace(node =>
                 {
                     if (node is XrefInline xref)
                     {
                         var (uid, query, _) = HrefUtility.SplitHref(xref.Href);
                         var xrefSpec = resolveXref(uid);
                         if (xrefSpec is null)
                         {
                             var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                             var error = raw.StartsWith("@")
                                 ? Errors.AtUidNotFound((Document)InclusionContext.File, xref.Href, raw)
                                 : Errors.UidNotFound((Document)InclusionContext.File, xref.Href, raw);

                             Markup.Result.Errors.Add(error);
                             return new LiteralInline(raw);
                         }

                         string display;
                         var content = string.IsNullOrEmpty(xrefSpec.GetName()) ? xrefSpec.Uid : xrefSpec.GetName();
                         if (!string.IsNullOrEmpty(query))
                         {
                             var queries = HttpUtility.ParseQueryString(query.Substring(1));
                             var displayProperty = queries["displayProperty"];
                             if (!string.IsNullOrEmpty(displayProperty))
                             {
                                 display = xrefSpec.GetXrefPropertyValue(displayProperty);
                             }
                             else
                             {
                                 display = content;
                             }
                         }
                         else
                         {
                             display = content;
                         }

                         return new LinkInline(xrefSpec.Href, null).AppendChild(new LiteralInline(display));
                     }
                     return node;
                 });
             });
        }
    }
}
