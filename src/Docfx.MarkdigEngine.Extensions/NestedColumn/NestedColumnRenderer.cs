// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class NestedColumnRender : HtmlObjectRenderer<NestedColumnBlock>
{
    protected override void Write(HtmlRenderer renderer, NestedColumnBlock obj)
    {
        renderer.Write("<div class=\"column");

        if (obj.ColumnWidth != "1")
        {
            renderer.Write($" span{obj.ColumnWidth}");
        }
        renderer.Write("\"").WriteAttributes(obj);
        renderer.WriteLine(">");
        renderer.WriteChildren(obj);
        renderer.WriteLine("</div>");
    }
}
