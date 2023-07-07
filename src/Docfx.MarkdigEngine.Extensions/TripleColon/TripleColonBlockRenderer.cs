// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class TripleColonBlockRenderer : HtmlObjectRenderer<TripleColonBlock>
{
    private readonly MarkdownContext _context;

    public TripleColonBlockRenderer(MarkdownContext context)
    {
        _context = context;
    }

    protected override void Write(HtmlRenderer renderer, TripleColonBlock block)
    {
        var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{block.Extension.Name}", message, block));

        if (block.Extension.Render(renderer, block, logWarning))
        {
            return;
        }

        renderer.Write("<div").WriteAttributes(block).WriteLine(">");
        renderer.WriteChildren(block);
        renderer.WriteLine("</div>");
    }
}
