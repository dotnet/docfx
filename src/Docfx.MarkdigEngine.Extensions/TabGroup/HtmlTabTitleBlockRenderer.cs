// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class HtmlTabTitleBlockRenderer : HtmlObjectRenderer<TabTitleBlock>
{
    protected override void Write(HtmlRenderer renderer, TabTitleBlock block)
    {
        foreach (var inline in block.Inline)
        {
            renderer.Render(inline);
        }
    }
}
