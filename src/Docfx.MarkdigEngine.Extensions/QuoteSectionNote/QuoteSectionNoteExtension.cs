// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Newtonsoft.Json;

namespace Docfx.MarkdigEngine.Extensions;

public class QuoteSectionNoteExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;
    private readonly Dictionary<string, string> _notes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NOTE"] = "NOTE",
        ["TIP"] = "TIP",
        ["WARNING"] = "WARNING",
        ["IMPORTANT"] = "IMPORTANT",
        ["CAUTION"] = "CAUTION",
    };

    public QuoteSectionNoteExtension(MarkdownContext context)
    {
        _context = context;

        var config = _context.GetExtensionConfiguration("Alerts");
        if (config != null)
        {
            foreach (var (key, value) in config)
                _notes[key] = value;
        }
    }

    void IMarkdownExtension.Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Replace<QuoteBlockParser>(new QuoteSectionNoteParser(_context, _notes.Keys.ToArray())))
        {
            pipeline.BlockParsers.Insert(0, new QuoteSectionNoteParser(_context, _notes.Keys.ToArray()));
        }
    }

    void IMarkdownExtension.Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer)
        {
            QuoteSectionNoteRender quoteSectionNoteRender = new(_context, _notes);

            if (!renderer.ObjectRenderers.Replace<QuoteBlockRenderer>(quoteSectionNoteRender))
            {
                renderer.ObjectRenderers.Insert(0, quoteSectionNoteRender);
            }
        }
    }
}
