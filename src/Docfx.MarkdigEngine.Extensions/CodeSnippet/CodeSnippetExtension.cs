// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class CodeSnippetExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public CodeSnippetExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.BlockParsers.AddIfNotAlready<CodeSnippetParser>();
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<HtmlCodeSnippetRenderer>())
        {
            // Must be inserted before CodeBlockRenderer
            htmlRenderer.ObjectRenderers.Insert(0, new HtmlCodeSnippetRenderer(_context));
        }
    }
}
