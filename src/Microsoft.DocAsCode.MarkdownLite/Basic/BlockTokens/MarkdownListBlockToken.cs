// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownListBlockToken : IMarkdownToken
    {
        public MarkdownListBlockToken(IMarkdownRule rule, ImmutableArray<IMarkdownToken> tokens, bool ordered)
        {
            Rule = rule;
            Tokens = tokens;
            Ordered = ordered;
        }

        public IMarkdownRule Rule { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }

        public bool Ordered { get; }

        public string RawMarkdown { get; set; }
    }
}
