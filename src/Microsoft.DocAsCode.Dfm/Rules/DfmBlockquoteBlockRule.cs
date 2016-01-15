// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "DfmBlockquote";

        public override IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var c = engine.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
            var result = base.TryMatch(engine, ref source);
            engine.SwitchContext(c);
            return result;
        }
    }
}
