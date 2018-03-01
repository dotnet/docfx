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
