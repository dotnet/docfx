// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class MonikerRangeExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public MonikerRangeExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (pipeline.BlockParsers.Contains<CustomContainerParser>())
        {
            pipeline.BlockParsers.InsertBefore<CustomContainerParser>(new MonikerRangeParser(_context));
        }
        else
        {
            pipeline.BlockParsers.AddIfNotAlready(new MonikerRangeParser(_context));
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<MonikerRangeRender>())
        {
            htmlRenderer.ObjectRenderers.Insert(0, new MonikerRangeRender());
        }
    }
}
