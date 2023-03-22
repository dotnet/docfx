// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class InteractiveCodeExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var codeSnippetInteractiveRewriter = new CodeSnippetInteractiveRewriter();
        var fencedCodeInteractiveRewriter = new FencedCodeInteractiveRewriter();

        var codeSnippetVisitor = new MarkdownDocumentVisitor(codeSnippetInteractiveRewriter);
        var fencedCodeVisitor = new MarkdownDocumentVisitor(fencedCodeInteractiveRewriter);

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
