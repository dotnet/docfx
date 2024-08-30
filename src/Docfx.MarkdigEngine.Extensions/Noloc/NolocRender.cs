// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class NolocRender : HtmlObjectRenderer<NolocInline>
{
    protected override void Write(HtmlRenderer renderer, NolocInline obj)
    {
        renderer.Write(obj.Text);
    }
}
