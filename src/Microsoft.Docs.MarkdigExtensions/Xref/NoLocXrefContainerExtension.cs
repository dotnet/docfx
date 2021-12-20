// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Renderers;

namespace Microsoft.Docs.MarkdigExtensions;

public class NoLocXrefContainerExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        var htmlRenderer = renderer as HtmlRenderer;
        htmlRenderer.ObjectRenderers.AddIfNotAlready<NoLocXrefContainerRenderer>();
    }
}
