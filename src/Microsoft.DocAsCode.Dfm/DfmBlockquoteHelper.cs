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
                if (token is DfmSectionBlockToken || token is DfmNoteBlockToken)
                {
                    splitToken = new SplitToken(token);
                    splitTokens.Add(splitToken);
                }
                else
                {
                    if (splitToken != null)
                    {
                        splitToken.InnerTokens.Add(token);
                        continue;
                    }
                    splitToken = new SplitToken(token);
                    splitToken.InnerTokens.Add(token);
                    splitTokens.Add(splitToken);
                }
            }
            return splitTokens;
        }
    }
}
