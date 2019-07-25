// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class ImageRender : HtmlObjectRenderer<ImageBlock>
    {
        protected override void Write(HtmlRenderer renderer, ImageBlock obj)
        {
            renderer.Write($"<img src=\"{obj.Src}\" alt=\"{obj.Alt}\" aria-describedby=\"{obj.Id}\">");
            renderer.WriteLine($"<div id=\"{obj.Id}\" class=\"visually-hidden\">");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</div>");
        }
    }
}