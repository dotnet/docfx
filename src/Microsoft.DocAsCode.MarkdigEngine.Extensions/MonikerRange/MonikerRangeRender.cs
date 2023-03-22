// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class MonikerRangeRender : HtmlObjectRenderer<MonikerRangeBlock>
{
    protected override void Write(HtmlRenderer renderer, MonikerRangeBlock obj)
    {
        renderer.Write("<div").WriteAttributes(obj).WriteLine(">");
        renderer.WriteChildren(obj);
        renderer.WriteLine("</div>");
    }
}
