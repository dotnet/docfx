// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownLinkInlineToken : IMarkdownToken, IMarkdownRewritable<MarkdownLinkInlineToken>
    {
        public MarkdownLinkInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string title, ImmutableArray<IMarkdownToken> content, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Title = title;
            Content = content;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Href { get; }

        public string Title { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public bool ShouldApplyInlineRule { get; set; }

        public string RawMarkdown { get; set; }

        public MarkdownLinkInlineToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new MarkdownLinkInlineToken(Rule, Context, Href, Title, tokens, RawMarkdown);
        }
    }
}
