// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions;

public class NolocRenderer : HtmlObjectRenderer<NolocInline>
{
    protected override void Write(HtmlRenderer renderer, NolocInline obj)
    {
        renderer.Write($"<span class=\"no-loc\" dir=\"ltr\" lang=\"en-us\">{ExtensionsHelper.Escape(obj.Text)}</span>");
    }
}
