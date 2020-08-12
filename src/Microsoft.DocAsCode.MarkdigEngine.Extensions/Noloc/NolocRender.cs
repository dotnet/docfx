// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    public class NolocRender : HtmlObjectRenderer<NolocInline>
    {
        protected override void Write(HtmlRenderer renderer, NolocInline obj)
        {
            renderer.Write(obj.Text);
        }
    }
}
