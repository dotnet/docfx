// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class NestedColumnRender : HtmlObjectRenderer<NestedColumnBlock>
    {
        protected override void Write(HtmlRenderer renderer, NestedColumnBlock obj)
        {
            renderer.Write("<div class=\"column").WriteAttributes(obj);

            if (obj.ColumnWidth != "1")
            {
                renderer.Write($" span{obj.ColumnWidth}");
            }

            renderer.WriteLine("\">");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</div>");
        }
    }
}
