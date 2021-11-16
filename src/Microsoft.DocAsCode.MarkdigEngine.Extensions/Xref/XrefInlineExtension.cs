// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Parsers.Inlines;
    using Markdig.Renderers;

    public class XrefInlineExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.InlineParsers.InsertBefore<AutolinkInlineParser>(new XrefInlineParser());
            pipeline.InlineParsers.AddIfNotAlready(new XrefInlineShortParser());
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<HtmlXrefInlineRender>())
            {
                // Must be inserted before CodeBlockRenderer
                htmlRenderer.ObjectRenderers.Insert(0, new HtmlXrefInlineRender());
            }
        }
    }
}
