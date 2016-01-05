// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownNonParagraphBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownNonParagraphBlockToken>
    {
        public MarkdownNonParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public string RawMarkdown { get; set; }

        public MarkdownNonParagraphBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var content = Content.Rewrite(rewriteEngine);
            if (content == Content)
            {
                return this;
            }
            return new MarkdownNonParagraphBlockToken(Rule, Context, content, RawMarkdown);
        }
    }
}
