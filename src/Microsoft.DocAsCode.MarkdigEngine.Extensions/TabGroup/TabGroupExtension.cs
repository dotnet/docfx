// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

    public class TabGroupExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var tabGroupAggregator = new TabGroupAggregator();
            var aggregateVisitor = new MarkdownDocumentAggregatorVisitor(tabGroupAggregator);

            var tagGroupIdRewriter = new TabGroupIdRewriter();
            var tagGroupIdVisitor = new MarkdownDocumentVisitor(tagGroupIdRewriter);

            var activeAndVisibleRewriter = new ActiveAndVisibleRewriter();
            var activeAndVisibleVisitor = new MarkdownDocumentVisitor(activeAndVisibleRewriter);

            pipeline.DocumentProcessed += document =>
            {
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
