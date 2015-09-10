// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using System.Xml;

    public class DocfxFlavoredOptions : Options
    {
        public DocfxFlavoredOptions() : base()
        {
            this.Renderer = new DocfxFlavoredRenderer(this);
        }
    }
}
