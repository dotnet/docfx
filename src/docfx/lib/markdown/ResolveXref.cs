// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
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
                        var xrefSpec = resolveXref(xref.Href);
                        if (xrefSpec is null)
                        {
                            var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                            var error = raw.StartsWith("@")
                                ? Errors.AtUidNotFound((Document)InclusionContext.File, xref.Href, raw)
                                : Errors.UidNotFound((Document)InclusionContext.File, xref.Href, raw);

                            Markup.Result.Errors.Add(error);
                            return new LiteralInline(raw);
                        }

                        // TODO: Support advanced cross reference
                        // e.g.: <a href="xref:System.String?displayProperty=fullName"/>
                        var content = new LiteralInline(string.IsNullOrEmpty(xrefSpec.Name) ? xrefSpec.Uid : xrefSpec.Name);
                        return new LinkInline(xrefSpec.Href, null).AppendChild(content);
                    }
                    return node;
                });
            });
        }
    }
}
