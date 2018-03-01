// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmTabContentBlockToken : IMarkdownExpression, IMarkdownRewritable<DfmTabContentBlockToken>
    {
        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public SourceInfo SourceInfo { get; }

        public DfmTabContentBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> content, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            SourceInfo = sourceInfo;
        }

        public IEnumerable<IMarkdownToken> GetChildren()
        {
            return Content;
        }

        public DfmTabContentBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var tokens = rewriteEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new DfmTabContentBlockToken(Rule, Context, tokens, SourceInfo);
        }
    }
}
