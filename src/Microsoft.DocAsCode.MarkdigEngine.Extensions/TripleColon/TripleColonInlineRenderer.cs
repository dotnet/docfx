// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class TripleColonInlineRenderer : HtmlObjectRenderer<TripleColonInline>
    {
        protected override void Write(HtmlRenderer renderer, TripleColonInline inline)
        {
            if (inline.Extension.Render(renderer, inline))
            {
                return;
            }

            renderer.Write("<div").WriteAttributes(inline).WriteLine(">");
            renderer.WriteChildren(inline);
            renderer.WriteLine("</div>");
        }
    }
}
