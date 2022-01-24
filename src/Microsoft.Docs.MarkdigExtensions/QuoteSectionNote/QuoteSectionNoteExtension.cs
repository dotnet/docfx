// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class QuoteSectionNoteExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public QuoteSectionNoteExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Replace<QuoteBlockParser>(new QuoteSectionNoteParser(_context)))
        {
            pipeline.BlockParsers.Insert(0, new QuoteSectionNoteParser(_context));
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer)
        {
            var quoteSectionNoteRender = new QuoteSectionNoteRender(_context);

            if (!renderer.ObjectRenderers.Replace<QuoteBlockRenderer>(quoteSectionNoteRender))
            {
                renderer.ObjectRenderers.Insert(0, quoteSectionNoteRender);
            }
        }
    }
}
