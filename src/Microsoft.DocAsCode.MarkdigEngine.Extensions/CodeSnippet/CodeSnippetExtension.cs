// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

    public class CodeSnippetExtension : IMarkdownExtension
    {
        private IMarkdownEngine _engine;
        private MarkdownContext _context;

        public CodeSnippetExtension(IMarkdownEngine engine, MarkdownContext context)
        {
            _engine = engine;
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.BlockParsers.AddIfNotAlready<CodeSnippetParser>();
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null && !htmlRenderer.ObjectRenderers.Contains<HtmlCodeSnippetRenderer>())
            {
                // Must be inserted before CodeBlockRenderer
                htmlRenderer.ObjectRenderers.Insert(0, new HtmlCodeSnippetRenderer(_engine, _context));
            }
        }
    }
}
