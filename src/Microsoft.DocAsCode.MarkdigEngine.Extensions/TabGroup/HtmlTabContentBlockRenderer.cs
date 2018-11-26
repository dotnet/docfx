// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Renderers;
    using Markdig.Renderers.Html;
    using Markdig.Syntax;

    class HtmlTabContentBlockRenderer : HtmlObjectRenderer<TabContentBlock>
    {
        protected override void Write(HtmlRenderer renderer, TabContentBlock block)
        {
            foreach(var item in block)
            {
                if (!(item is ThematicBreakBlock))
                {
                    renderer.Render(item);
                }
            }
        }
    }
}
