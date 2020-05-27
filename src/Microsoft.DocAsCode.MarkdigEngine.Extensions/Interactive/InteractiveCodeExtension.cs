// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig;
    using Markdig.Renderers;

    public class InteractiveCodeExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            var codeSnippetInteractiveRewriter = new CodeSnippetInteractiveRewriter();
            var fencedCodeInteractiveRewrtier = new FencedCodeInteractiveRewriter();

            var codeSnippetVisitor = new MarkdownDocumentVisitor(codeSnippetInteractiveRewriter);
            var fencedCodeVisitor = new MarkdownDocumentVisitor(fencedCodeInteractiveRewrtier);

            pipeline.DocumentProcessed += document =>
            {
                codeSnippetVisitor.Visit(document);
                fencedCodeVisitor.Visit(document);
            };
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {

        }
    }
}
