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
        Func<SourceInfo<string>?, SourceInfo<string>?, bool, XrefLink> resolveXref)
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
                var xrefResolvingResult = resolveXref(source, null, suppressXrefNotFound);

                if (xrefResolvingResult.Href is null)
                {
                    return new LiteralInline(raw);
                }

                var linkInline = new LinkInline(xrefResolvingResult.Href, null);
                if (!xrefResolvingResult.Localizable)
                {
                    var attributes = linkInline.GetAttributes();
                    attributes.AddClass("no-loc");
                    linkInline.SetAttributes(attributes);
                }
                linkInline.AppendChild(new LiteralInline(xrefResolvingResult.Display));
                return linkInline;
            }
            return node;
        }));
    }
}
