// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownListBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownListBlockToken>
    {
        public MarkdownListBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> tokens, bool ordered, SourceInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            Tokens = tokens;
            Ordered = ordered;
            SourceInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }

        public bool Ordered { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownListBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Tokens);
            if (tokens == Tokens)
            {
                return this;
            }
            return new MarkdownListBlockToken(Rule, Context, tokens, Ordered, SourceInfo);
        }
    }
}
