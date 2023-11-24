// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

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
