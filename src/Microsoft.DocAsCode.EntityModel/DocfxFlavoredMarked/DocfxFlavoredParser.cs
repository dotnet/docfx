// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using System.Collections.Generic;

    public class DocfxFlavoredParser : Parser
    {
        public DocfxFlavoredParser(Options options) : base(options, new DocfxFlavoredInlineLexer(options))
        {
        }

        public virtual string Parse(TokensResult src, Stack<string> parents)
        {
            ((DocfxFlavoredInlineLexer)this.Inline).Parents = parents;
            return base.Parse(src);
        }
    }
}
