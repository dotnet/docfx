// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTableItemBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownTableItemBlockToken>
    {
        public MarkdownTableItemBlockToken(
            IMarkdownRule rule,
            IMarkdownContext context,
            InlineContent content,
            SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public IMarkdownContext Context { get; }

        public IMarkdownRule Rule { get; }

        public SourceInfo SourceInfo { get; }

        public InlineContent Content { get; }

        public MarkdownTableItemBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var rewritten = Content.Rewrite(rewriterEngine);
            if (rewritten == Content)
            {
                return this;
            }
            return new MarkdownTableItemBlockToken(Rule, Context, rewritten, SourceInfo);
        }

    }
}
