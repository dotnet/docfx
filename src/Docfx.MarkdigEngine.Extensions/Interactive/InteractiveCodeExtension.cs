// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

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
