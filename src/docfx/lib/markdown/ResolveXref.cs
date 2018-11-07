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

                         // fallback order:
                         // xrefSpec.displayPropertyName -> xrefSpec.name -> uid
                         string displayPropertyValue = null;
                         var name = xrefSpec.GetXrefPropertyValue("name");
                         if (!string.IsNullOrEmpty(query))
                         {
                             displayPropertyValue = xrefSpec.GetXrefPropertyValue(HttpUtility.ParseQueryString(query.Substring(1))?["displayProperty"]);
                         }
                         string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
                         return new LinkInline(xrefSpec.Href, null).AppendChild(new LiteralInline(display));
                     }
                     return node;
                 });
             });
        }
    }
}
