// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Extensions.CustomContainers;
    using Markdig.Renderers;

    public class MonikerRangeExtension : IMarkdownExtension
    {
        private readonly MarkdownContext _context;

        public MonikerRangeExtension(MarkdownContext context)
        {
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (pipeline.BlockParsers.Contains<CustomContainerParser>())
            {
                pipeline.BlockParsers.InsertBefore<CustomContainerParser>(new MonikerRangeParser(_context));
            }
            else
            {
                pipeline.BlockParsers.AddIfNotAlready(new MonikerRangeParser(_context));
            }            
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null && !htmlRenderer.ObjectRenderers.Contains<MonikerRangeRender>())
            {
                htmlRenderer.ObjectRenderers.Insert(0, new MonikerRangeRender());
            }
        }
    }
}
