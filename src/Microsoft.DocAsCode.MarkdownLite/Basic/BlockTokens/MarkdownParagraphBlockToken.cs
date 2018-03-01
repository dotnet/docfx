// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class MarkdownParagraphBlockToken : IMarkdownExpression, IMarkdownRewritable<MarkdownParagraphBlockToken>
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent inlineTokens, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            InlineTokens = inlineTokens;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent InlineTokens { get; }

        public SourceInfo SourceInfo { get; }

        public static MarkdownParagraphBlockToken Create(IMarkdownRule rule, MarkdownParser engine, string content, SourceInfo sourceInfo)
        {
            return new MarkdownParagraphBlockToken(rule, engine.Context, engine.TokenizeInline(sourceInfo.Copy(content)), sourceInfo);
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

        public IEnumerable<IMarkdownToken> GetChildren() => InlineTokens.Tokens;
    }
}
