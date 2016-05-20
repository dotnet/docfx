// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownParagraphBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownParagraphBlockToken>
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent inlineTokens, string rawMarkdown, LineInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            InlineTokens = inlineTokens;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent InlineTokens { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }

        public static MarkdownParagraphBlockToken Create(IMarkdownRule rule, MarkdownParser engine, string content, string rawMarkdown, LineInfo lineInfo)
        {
            return new MarkdownParagraphBlockToken(rule, engine.Context, engine.TokenizeInline(content, lineInfo), rawMarkdown, lineInfo);
        }

        public MarkdownParagraphBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var c = InlineTokens.Rewrite(rewriterEngine);
            if (c == InlineTokens)
            {
                return this;
            }
            return new MarkdownParagraphBlockToken(Rule, Context, c, RawMarkdown, LineInfo);
        }
    }
}
