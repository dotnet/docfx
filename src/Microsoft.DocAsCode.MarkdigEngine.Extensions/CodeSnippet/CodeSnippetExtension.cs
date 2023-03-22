// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

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
