// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class XrefExtension
{
    public static MarkdownPipelineBuilder UseXref(
        this MarkdownPipelineBuilder builder,
        Func<SourceInfo<string>?, SourceInfo<string>?, bool, (string? href, string display)> resolveXref)
    {
        return builder.Use(document => document.Replace(node =>
        {
            // <xref:uid>
            // @uid
            if (node is XrefInline xref)
            {
                var raw = xref.GetAttributes().Properties.First(p => p.Key == "data-raw-source").Value;
                var suppressXrefNotFound = raw.StartsWith("@");
                var source = new SourceInfo<string>(xref.Href, xref.GetSourceInfo());
                var (href, display) = resolveXref(source, null, suppressXrefNotFound);

                if (href is null)
                {
                    return new LiteralInline(raw);
                }

                return new LinkInline(href, null).AppendChild(new LiteralInline(display));
            }
            return node;
        }));
    }
}
