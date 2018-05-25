// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class RenderZoneRender : HtmlObjectRenderer<RenderZoneBlock>
    {
        protected override void Write(HtmlRenderer renderer, RenderZoneBlock obj)
        {
            renderer.Write("<div").WriteAttributes(obj).Write($" data-zone=\"{obj.Target}\"").WriteLine(">");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</div>");
        }
    }
}
