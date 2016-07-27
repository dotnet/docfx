// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class MarkdownNonParagraphBlockToken : IMarkdownExpression, IMarkdownRewritable<MarkdownNonParagraphBlockToken>
    {
        public MarkdownNonParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownNonParagraphBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var content = Content.Rewrite(rewriteEngine);
            if (content == Content)
            {
                return this;
            }
            return new MarkdownNonParagraphBlockToken(Rule, Context, content, SourceInfo);
        }

        public IEnumerable<IMarkdownToken> GetChildren() => Content.Tokens;
    }
}
