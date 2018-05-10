// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

    public class ValidationExtension : IMarkdownExtension
    {
        private readonly MarkdownValidatorBuilder _mvb;

        private readonly MarkdownContext _context;

        public ValidationExtension(MarkdownValidatorBuilder validationBuilder, MarkdownContext context)
        {
            _mvb = validationBuilder;
            _context = context;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var tokenRewriter = _mvb.CreateRewriter(_context);
            var visitor = new MarkdownDocumentVisitor(tokenRewriter);

            pipeline.DocumentProcessed += document =>
            {
                visitor.Visit(document);
            };
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {

        }
    }
}
