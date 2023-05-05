// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

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

    public QuoteSectionNoteExtension(MarkdownContext context, Dictionary<string, string> notes = null)
    {
        _context = context;

        if (notes != null)
        {
            foreach (var (key, value) in notes)
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
