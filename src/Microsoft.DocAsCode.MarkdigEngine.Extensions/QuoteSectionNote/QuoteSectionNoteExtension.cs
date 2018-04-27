// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Microsoft.DocAsCode.Common;

    using Markdig;
    using Markdig.Parsers;
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class QuoteSectionNoteExtension : IMarkdownExtension
    {
        private MarkdownContext _context;

        public QuoteSectionNoteExtension(MarkdownContext context)
        {
            _context = context;
        }

        void IMarkdownExtension.Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Replace<QuoteBlockParser>(new QuoteSectionNoteParser()))
            {
                Logger.LogWarning($"Can't find QuoteBlockParser to replace, insert QuoteSectionNoteParser directly.");
                pipeline.BlockParsers.Insert(0, new QuoteSectionNoteParser());
            }
        }

        void IMarkdownExtension.Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            var htmlRenderer = renderer as HtmlRenderer;
            if (htmlRenderer != null)
            {
                QuoteSectionNoteRender quoteSectionNoteRender = new QuoteSectionNoteRender(_context.Tokens);

                if (!renderer.ObjectRenderers.Replace<QuoteBlockRenderer>(quoteSectionNoteRender))
                {
                    Logger.LogWarning($"Can't find QuoteBlockRenderer to replace, insert QuoteSectionNoteRender directly.");
                    renderer.ObjectRenderers.Insert(0, quoteSectionNoteRender);
                }
            }
        }
    }
}
