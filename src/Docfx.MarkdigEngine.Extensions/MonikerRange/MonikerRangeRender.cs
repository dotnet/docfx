// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class MonikerRangeRender : HtmlObjectRenderer<MonikerRangeBlock>
{
    protected override void Write(HtmlRenderer renderer, MonikerRangeBlock obj)
    {
        renderer.Write("<div").WriteAttributes(obj).WriteLine(">");
        renderer.WriteChildren(obj);
        renderer.WriteLine("</div>");
    }
}
