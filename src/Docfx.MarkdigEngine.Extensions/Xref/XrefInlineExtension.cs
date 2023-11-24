// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Parsers.Inlines;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class XrefInlineExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.InlineParsers.InsertBefore<AutolinkInlineParser>(new XrefInlineParser());
        pipeline.InlineParsers.AddIfNotAlready(new XrefInlineShortParser());
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<HtmlXrefInlineRender>())
        {
            // Must be inserted before CodeBlockRenderer
            htmlRenderer.ObjectRenderers.Insert(0, new HtmlXrefInlineRender());
        }
    }
}
