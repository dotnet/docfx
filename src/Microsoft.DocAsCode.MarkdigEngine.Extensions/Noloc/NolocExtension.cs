// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions;

public class NolocExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        pipeline.InlineParsers.AddIfNotAlready<NolocParser>();
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        var htmlRenderer = renderer as HtmlRenderer;
        htmlRenderer.ObjectRenderers.AddIfNotAlready<NolocRender>();
    }
}