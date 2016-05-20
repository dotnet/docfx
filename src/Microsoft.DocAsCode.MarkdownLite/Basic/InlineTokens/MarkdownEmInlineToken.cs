// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownEmInlineToken : IMarkdownToken, IMarkdownRewritable<MarkdownEmInlineToken>
    {
        public MarkdownEmInlineToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> content, SourceInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            SourceInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownEmInlineToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new MarkdownEmInlineToken(Rule, Context, tokens, SourceInfo);
        }
    }
}
