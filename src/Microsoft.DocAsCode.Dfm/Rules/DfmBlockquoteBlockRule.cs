// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "DfmBlockquote";

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var c = parser.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
            var result = base.TryMatch(parser, context);
            parser.SwitchContext(c);
            return result;
        }
    }
}
