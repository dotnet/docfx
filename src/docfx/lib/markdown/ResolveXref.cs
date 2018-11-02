// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            return MarkdigUtility.Use(builder, document =>
             {
                 MarkdigUtility.Replace(document, node =>
                 {
                     if (node is XrefInline xref)
                     {
                         var (uid, query, _) = HrefUtility.SplitHref(xref.Href);
                         var xrefSpec = resolveXref(uid);
                         if (xrefSpec is null)
                         {
                             var raw = xref.GetAttributes().Properties.First((KeyValuePair<string, string> p) => p.Key == "data-raw-source").Value;
                             var error = raw.StartsWith("@")
                                 ? Errors.AtUidNotFound((Document)InclusionContext.File, xref.Href, raw)
                                 : Errors.UidNotFound((Document)InclusionContext.File, xref.Href, raw);

                             Markup.Result.Errors.Add(error);
                             return new LiteralInline(raw);
                         }

                         var queries = HttpUtility.ParseQueryString(query);
                         var displayProperty = queries["dispalyProperty"];
                         string content;
                         if (displayProperty is null)
                         {
                             content = string.IsNullOrEmpty(xrefSpec.GetName()) ? xrefSpec.Uid : xrefSpec.GetName();
                         }
                         else
                         {
                             content = displayProperty;
                         }
                         return new LinkInline(xrefSpec.Href, null).AppendChild(new LiteralInline(content));
                     }
                     return node;
                 });
             });
        }
    }
}
