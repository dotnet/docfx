// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class RowExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public RowExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (pipeline.BlockParsers.Contains<CustomContainerParser>())
        {
            pipeline.BlockParsers.InsertBefore<CustomContainerParser>(new RowParser(_context));
        }
        else
        {
            pipeline.BlockParsers.AddIfNotAlready(new RowParser(_context));
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<RowRender>())
        {
            htmlRenderer.ObjectRenderers.Insert(0, new RowRender());
        }
    }
}
