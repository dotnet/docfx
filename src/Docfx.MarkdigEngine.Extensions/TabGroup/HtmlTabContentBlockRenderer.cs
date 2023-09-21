// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

class HtmlTabContentBlockRenderer : HtmlObjectRenderer<TabContentBlock>
{
    protected override void Write(HtmlRenderer renderer, TabContentBlock block)
    {
        foreach (var item in block)
        {
            if (item is not ThematicBreakBlock)
            {
                renderer.Render(item);
            }
        }
    }
}
