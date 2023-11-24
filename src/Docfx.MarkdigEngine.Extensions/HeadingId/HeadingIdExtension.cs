// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig;
using Markdig.Renderers;

namespace Docfx.MarkdigEngine.Extensions;

public class HeadingIdExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var tokenRewriter = new HeadingIdRewriter();
        var visitor = new MarkdownDocumentVisitor(tokenRewriter);

        pipeline.DocumentProcessed += visitor.Visit;
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {

    }
}
