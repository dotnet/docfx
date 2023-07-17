// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class RowRender : HtmlObjectRenderer<RowBlock>
{
    protected override void Write(HtmlRenderer renderer, RowBlock obj)
    {
        renderer.Write("<section class=\"row\"").WriteAttributes(obj).WriteLine(">");
        renderer.WriteChildren(obj);
        renderer.WriteLine("</section>");
    }
}
