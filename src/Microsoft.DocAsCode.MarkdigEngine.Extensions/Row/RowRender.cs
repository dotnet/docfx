// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class RowRender : HtmlObjectRenderer<RowBlock>
    {
        protected override void Write(HtmlRenderer renderer, RowBlock obj)
        {
            renderer.Write("<section class=\"row\"").WriteAttributes(obj).WriteLine(">");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</section>");
        }
    }
}
