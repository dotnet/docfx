// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownParagraphBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownParagraphBlockToken>
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent inlineTokens, SourceInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            InlineTokens = inlineTokens;
            SourceInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent InlineTokens { get; }

        public SourceInfo SourceInfo { get; }

        public static MarkdownParagraphBlockToken Create(IMarkdownRule rule, MarkdownParser engine, string content, SourceInfo lineInfo)
        {
            return new MarkdownParagraphBlockToken(rule, engine.Context, engine.TokenizeInline(lineInfo.Copy(content)), lineInfo);
        }

        public MarkdownParagraphBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var c = InlineTokens.Rewrite(rewriterEngine);
            if (c == InlineTokens)
            {
                return this;
            }
            return new MarkdownParagraphBlockToken(Rule, Context, c, SourceInfo);
        }
    }
}
