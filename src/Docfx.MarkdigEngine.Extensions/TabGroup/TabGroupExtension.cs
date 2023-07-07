// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class TabGroupExtension : IMarkdownExtension
{
    private readonly MarkdownContext _context;

    public TabGroupExtension(MarkdownContext context)
    {
        _context = context;
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.DocumentProcessed += document =>
        {
            var tabGroupAggregator = new TabGroupAggregator();
            var aggregateVisitor = new MarkdownDocumentAggregatorVisitor(tabGroupAggregator);

            var tagGroupIdRewriter = new TabGroupIdRewriter();
            var tagGroupIdVisitor = new MarkdownDocumentVisitor(tagGroupIdRewriter);

            var activeAndVisibleRewriter = new ActiveAndVisibleRewriter(_context);
            var activeAndVisibleVisitor = new MarkdownDocumentVisitor(activeAndVisibleRewriter);

            aggregateVisitor.Visit(document);
            tagGroupIdVisitor.Visit(document);
            activeAndVisibleVisitor.Visit(document);
        };
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            if (!htmlRenderer.ObjectRenderers.Contains<HtmlTabGroupBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new HtmlTabGroupBlockRenderer());
            }

            if (!htmlRenderer.ObjectRenderers.Contains<HtmlTabTitleBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new HtmlTabTitleBlockRenderer());
            }

            if (!htmlRenderer.ObjectRenderers.Contains<HtmlTabContentBlockRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new HtmlTabContentBlockRenderer());
            }
        }
    }
}
