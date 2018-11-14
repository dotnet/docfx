// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal class MonikerRangeRender : HtmlObjectRenderer<MonikerRangeBlock>
    {
        private readonly Func<string, List<string>> _parseMonikerRange;

        public MonikerRangeRender(Func<string, List<string>> parseMonikerRange)
           : base()
        {
            _parseMonikerRange = parseMonikerRange;
        }

        protected override void Write(HtmlRenderer renderer, MonikerRangeBlock obj)
        {
            renderer.Write("<div").WriteAttributes(obj).Write($" data-moniker=\"{string.Join(" ", _parseMonikerRange(obj.MonikerRange))}\"").WriteLine(">");
            renderer.WriteChildren(obj);
            renderer.WriteLine("</div>");
        }
    }
}
