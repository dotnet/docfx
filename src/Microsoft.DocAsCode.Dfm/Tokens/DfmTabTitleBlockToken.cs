// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTabTitleBlockToken : IMarkdownExpression, IMarkdownRewritable<DfmTabTitleBlockToken>
    {
        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public SourceInfo SourceInfo { get; }

        public DfmTabTitleBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public IEnumerable<IMarkdownToken> GetChildren()
        {
            return Content.Tokens;
        }

        public DfmTabTitleBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var tokens = rewriteEngine.Rewrite(Content.Tokens);
            if (tokens == Content.Tokens)
            {
                return this;
            }
            return new DfmTabTitleBlockToken(Rule, Context, new InlineContent(tokens), SourceInfo);
        }
    }
}
