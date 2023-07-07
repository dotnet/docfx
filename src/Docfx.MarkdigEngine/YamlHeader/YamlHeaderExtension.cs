// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.MarkdigEngine.Extensions;

public class YamlHeaderExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public bool AllowInMiddleOfDocument { get; init; }

    public YamlHeaderExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<YamlFrontMatterParser>())
        {
            // Insert the YAML parser before the thematic break parser, as it is also triggered on a --- dash
            pipeline.BlockParsers.InsertBefore<ThematicBreakParser>(new YamlFrontMatterParser { AllowInMiddleOfDocument = AllowInMiddleOfDocument });
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (!renderer.ObjectRenderers.Contains<YamlHeaderRenderer>())
        {
            renderer.ObjectRenderers.InsertBefore<CodeBlockRenderer>(new YamlHeaderRenderer(_context));
        }
    }
}
