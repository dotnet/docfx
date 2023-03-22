// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class HeadingIdExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        var tokenRewriter = new HeadingIdRewriter();
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
