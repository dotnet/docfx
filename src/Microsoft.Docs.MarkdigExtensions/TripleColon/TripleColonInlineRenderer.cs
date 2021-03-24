// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Microsoft.Docs.MarkdigExtensions
{
    public class TripleColonInlineRenderer : HtmlObjectRenderer<TripleColonInline>
    {
        private readonly MarkdownContext _context;

        public TripleColonInlineRenderer(MarkdownContext context)
        {
            _context = context;
        }

        protected override void Write(HtmlRenderer renderer, TripleColonInline inline)
        {
            var logWarning = new Action<string>(message => _context.LogWarning($"invalid-{inline.Extension.Name}", message, inline));

            if (inline.Extension.Render(renderer, inline, logWarning))
            {
                return;
            }

            renderer.Write("<div").WriteAttributes(inline).WriteLine(">");
            renderer.WriteLine("</div>");
        }
    }
}
