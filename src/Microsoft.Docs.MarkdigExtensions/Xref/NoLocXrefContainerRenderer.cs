// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class NoLocXrefContainerRenderer : HtmlObjectRenderer<NoLocXrefContainer>
{
    protected override void Write(HtmlRenderer renderer, NoLocXrefContainer obj)
    {
        renderer.Write($"<span class=\"no-loc\">{ExtensionsHelper.Escape(obj.Content)}</span>");
    }
}
