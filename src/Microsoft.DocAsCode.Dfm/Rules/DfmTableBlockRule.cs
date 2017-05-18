// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTableBlockRule : MarkdownTableBlockRule
    {
        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var token = base.TryMatch(parser, context);
            if (token is TwoPhaseBlockToken tp)
            {
                return new TwoPhaseBlockToken(tp, tp.Context.SetIsInTable());
            }
            return token;
        }
    }
}
