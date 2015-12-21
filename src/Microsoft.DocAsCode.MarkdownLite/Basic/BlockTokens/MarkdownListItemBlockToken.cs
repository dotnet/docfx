// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownListItemBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownListItemBlockToken>
    {
        public MarkdownListItemBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> tokens, bool loose)
        {
            Rule = rule;
            Context = context;
            Tokens = tokens;
            Loose = loose;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Tokens { get; }

        public bool Loose { get; }

        public string RawMarkdown { get; set; }

        public MarkdownListItemBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Tokens);
            if (tokens == Tokens)
            {
                return this;
            }
            return new MarkdownListItemBlockToken(Rule, Context, tokens, Loose);
        }
    }
}
