// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmXrefInlineToken : IMarkdownExpression, IMarkdownRewritable<DfmXrefInlineToken>
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Href { get; }
        public ImmutableArray<IMarkdownToken> Content { get; }
        public string Title { get; }
        public bool ThrowIfNotResolved { get; }
        public SourceInfo SourceInfo { get; }

        public DfmXrefInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, ImmutableArray<IMarkdownToken> content, string title, bool throwIfNotResolved, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Content = content;
            Title = title;
            ThrowIfNotResolved = throwIfNotResolved;
            SourceInfo = sourceInfo;
        }

        public DfmXrefInlineToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var tokens = rewriteEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new DfmXrefInlineToken(Rule, Context, Href, tokens, Title, ThrowIfNotResolved, SourceInfo);
        }

        public IEnumerable<IMarkdownToken> GetChildren() => Content;
    }
}
