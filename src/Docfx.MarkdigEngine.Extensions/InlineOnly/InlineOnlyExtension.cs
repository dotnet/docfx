// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class InlineOnlyExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var paragraphBlockParser = pipeline.BlockParsers.FindExact<ParagraphBlockParser>() ?? new ParagraphBlockParser();
        pipeline.BlockParsers.Clear();
        pipeline.BlockParsers.Add(paragraphBlockParser);
        pipeline.BlockParsers.Add(new YamlFrontMatterParser());
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ImplicitParagraph = true;
        }
    }
}
