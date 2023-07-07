// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Extensions.CustomContainers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class TripleColonExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;
    private readonly IDictionary<string, ITripleColonExtensionInfo> _extensions;
    private readonly IDictionary<string, ITripleColonExtensionInfo> _extensionsInline;

    public TripleColonExtension(MarkdownContext context)
    {
        _context = context;
        _extensions = (new ITripleColonExtensionInfo[]
        {
            new ZoneExtension(),
            new ChromelessFormExtension(),
            new ImageExtension(context),
            new CodeExtension(context),
            // todo: moniker range, row, etc...
        }).ToDictionary(x => x.Name);

        _extensionsInline = (new ITripleColonExtensionInfo[]
        {
            new ImageExtension(context),
            new VideoExtension()
        }).ToDictionary(x => x.Name);
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var parser = new TripleColonBlockParser(_context, _extensions);
        if (pipeline.BlockParsers.Contains<CustomContainerParser>())
        {
            pipeline.BlockParsers.InsertBefore<CustomContainerParser>(parser);
        }
        else
        {
            pipeline.BlockParsers.AddIfNotAlready(parser);
        }

        var inlineParser = new TripleColonInlineParser(_context, _extensionsInline);
        pipeline.InlineParsers.InsertBefore<InlineParser>(inlineParser);

    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<TripleColonBlockRenderer>())
        {
            htmlRenderer.ObjectRenderers.Insert(0, new TripleColonInlineRenderer(_context));
            htmlRenderer.ObjectRenderers.Insert(0, new TripleColonBlockRenderer(_context));
        }
    }
}

public interface ITripleColonExtensionInfo
{
    string Name { get; }
    bool SelfClosing { get; }
    bool TryProcessAttributes(IDictionary<string, string> attributes, out HtmlAttributes htmlAttributes, out IDictionary<string, string> renderProperties, Action<string> logError, Action<string> logWarning, MarkdownObject markdownObject);
    bool TryValidateAncestry(ContainerBlock container, Action<string> logError);
    bool Render(HtmlRenderer renderer, MarkdownObject markdownObject, Action<string> logWarning);
}
