// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class TripleColonInlineRenderer : HtmlObjectRenderer<TripleColonInline>
{
    private readonly MarkdownContext _context;

    public TripleColonInlineRenderer(MarkdownContext context)
    {
        _context = context;
    }

    protected override void Write(HtmlRenderer renderer, TripleColonInline inline)
    {
        var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{inline.Extension.Name}", message, inline));

        if (inline.Extension.Render(renderer, inline, logWarning))
        {
            return;
        }

        renderer.Write("<div").WriteAttributes(inline).WriteLine(">");
        renderer.WriteLine("</div>");
    }
}
