// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class ResolveXref
    {
        public static MarkdownPipelineBuilder UseResolveXref(this MarkdownPipelineBuilder builder, List<DocfxException> errors, Func<string, string> resolveXref)
        {
            return builder.Use(document =>
            {
                document.Replace(node =>
                {
                    if (node is XrefInline xref)
                    {
                        var href = resolveXref(xref.Href);
                        if (string.IsNullOrEmpty(href))
                        {
                            var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                            errors.Add(Errors.XrefNotFound((Document)InclusionContext.File, xref.Href, raw));
                            return new LiteralInline(raw);
                        }
                    }
                    return node;
                });
            });
        }
    }
}
