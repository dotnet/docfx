// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmBlockquoteHelper
    {
        public static List<SplitToken> SplitBlockquoteTokens(ImmutableArray<IMarkdownToken> tokens)
        {
            var splitTokens = new List<SplitToken>();
            SplitToken splitToken = null;
            foreach (var token in tokens)
            {
                if (token is IDfmBlockSpecialSplitToken)
                {
                    splitToken = CreateSplitToken(token);
                    splitTokens.Add(splitToken);
                }
                else
                {
                    if (splitToken != null)
                    {
                        splitToken.InnerTokens.Add(token);
                        continue;
                    }
                    splitToken = CreateSplitToken(token);
                    splitToken.InnerTokens.Add(token);
                    splitTokens.Add(splitToken);
                }
            }

            return splitTokens;
        }

        private static SplitToken CreateSplitToken(IMarkdownToken token)
        {
            if (token is DfmSectionBlockToken)
            {
                return new DfmSectionBlockSplitToken(token);
            }
            if (token is DfmNoteBlockToken)
            {
                return new DfmNoteBlockSplitToken(token);
            }
            if (token is DfmVideoBlockToken)
            {
                return new DfmVideoBlockSplitToken(token);
            }
            return new DfmDefaultBlockQuoteBlockSplitToken(token);
        }
    }
}
