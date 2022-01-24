// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class HtmlXrefInlineRender : HtmlObjectRenderer<XrefInline>
{
    protected override void Write(HtmlRenderer renderer, XrefInline obj)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("<xref href=\"").Write(obj.Href).Write("\"").WriteAttributes(obj).Write("></xref>");
        }
        else
        {
            foreach (var pair in obj.GetAttributes().Properties)
            {
                if (pair.Key == "data-raw-source")
                {
                    renderer.Write(pair.Value);
                    break;
                }
            }
        }
    }
}
