// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class TripleColonRenderer : HtmlObjectRenderer<TripleColonBlock>
    {
        protected override void Write(HtmlRenderer renderer, TripleColonBlock b)
        {
            if (b.Extension.Render(renderer, b))
            {
                return;
            }

            renderer.Write("<div").WriteAttributes(b).WriteLine(">");
            renderer.WriteChildren(b);
            renderer.WriteLine("</div>");
        }
    }
}
