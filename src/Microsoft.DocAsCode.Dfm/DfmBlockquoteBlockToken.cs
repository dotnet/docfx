// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Immutable;

    using MarkdownLite;

    public class DfmBlockquoteBlockToken : MarkdownBlockquoteBlockToken
    {
        public DfmBlockquoteBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> tokens, string rawMarkdown)
            : base(rule, context, tokens, rawMarkdown)
        {
        }
    }
}
