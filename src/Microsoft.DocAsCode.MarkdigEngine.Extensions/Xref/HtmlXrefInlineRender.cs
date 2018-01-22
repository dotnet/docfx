// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Markdig.Renderers;
    using Markdig.Renderers.Html;

    public class HtmlXrefInlineRender : HtmlObjectRenderer<XrefInline>
    {
        protected override void Write(HtmlRenderer renderer, XrefInline obj)
        {
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("<xref href=\"").Write(obj.Href).Write("\"").WriteAttributes(obj).Write("></xref>");
            }
        }
    }
}
