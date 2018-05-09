// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

    public class ValidationExtension : IMarkdownExtension
    {
        private readonly MarkdownValidatorBuilder _mvb;

        public ValidationExtension(MarkdownValidatorBuilder validationBuilder)
        {
            _mvb = validationBuilder;
        }

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var tokenRewriter = _mvb.CreateRewriter();
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
