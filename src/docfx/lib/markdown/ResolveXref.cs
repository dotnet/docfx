// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Specialized;
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
        public static MarkdownPipelineBuilder UseResolveXref(this MarkdownPipelineBuilder builder, Func<string, string, XrefSpec> resolveXref)
        {
            return builder.Use(document =>
             {
                 document.Replace(node =>
                 {
                     if (node is XrefInline xref)
                     {
                         var (uid, query, _) = HrefUtility.SplitHref(xref.Href);
                         string moniker = null;
                         NameValueCollection queries = null;
                         if (!string.IsNullOrEmpty(query))
                         {
                             queries = HttpUtility.ParseQueryString(query.Substring(1));
                             moniker = queries?["view"];
                         }

                         // need to url decode uid from input content
                         var xrefSpec = resolveXref(HttpUtility.UrlDecode(uid), moniker);
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
                         var name = xrefSpec.GetXrefPropertyValue("name");
                         var displayPropertyValue = xrefSpec.GetXrefPropertyValue(queries?["displayProperty"]);
                         string display = !string.IsNullOrEmpty(displayPropertyValue) ? displayPropertyValue : (!string.IsNullOrEmpty(name) ? name : uid);
                         var href = !string.IsNullOrEmpty(moniker) ? $"{xrefSpec.Href}?view={moniker}" : xrefSpec.Href;
                         return new LinkInline(href, null).AppendChild(new LiteralInline(display));
                     }
                     return node;
                 });
             });
        }
    }
}
