// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class NestedColumnRenderer : HtmlObjectRenderer<NestedColumnBlock>
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
