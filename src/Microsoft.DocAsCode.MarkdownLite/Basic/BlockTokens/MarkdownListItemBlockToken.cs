// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownListItemBlockToken : IMarkdownToken
    {
        public MarkdownListItemBlockToken(IMarkdownRule rule, ImmutableArray<IMarkdownToken> tokens)
        {
            Rule = rule;
            Tokens = tokens;
        }

        public IMarkdownRule Rule { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }
    }
}
