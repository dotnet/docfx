// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownParagraphBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownParagraphBlockToken>
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent inlineTokens, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            InlineTokens = inlineTokens;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent InlineTokens { get; }

        public string RawMarkdown { get; set; }

        public static MarkdownParagraphBlockToken Create(IMarkdownRule rule, MarkdownParser engine, string content, string rawMarkdown)
        {
            return new MarkdownParagraphBlockToken(rule, engine.Context, engine.TokenizeInline(content), rawMarkdown);
        }

        public MarkdownParagraphBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var c = InlineTokens.Rewrite(rewriterEngine);
            if (c == InlineTokens)
            {
                return this;
            }
            return new MarkdownParagraphBlockToken(Rule, Context, c, RawMarkdown);
        }
    }
}
