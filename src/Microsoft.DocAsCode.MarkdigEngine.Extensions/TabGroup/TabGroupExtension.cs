// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

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
}
