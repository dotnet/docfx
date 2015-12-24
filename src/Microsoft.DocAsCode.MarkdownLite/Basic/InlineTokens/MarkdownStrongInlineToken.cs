// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownStrongInlineToken : IMarkdownToken, IMarkdownRewritable<MarkdownStrongInlineToken>
    {
        public MarkdownStrongInlineToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> content, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public string RawMarkdown { get; set; }

        public MarkdownStrongInlineToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new MarkdownStrongInlineToken(Rule, Context, tokens, RawMarkdown);
        }
    }
}
