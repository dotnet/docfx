// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;

    public class MarkdownParagraphBlockToken : IMarkdownToken
    {
        public MarkdownParagraphBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> inlineTokens)
        {
            Rule = rule;
            Context = context;
            InlineTokens = inlineTokens;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> InlineTokens { get; }

        public string RawMarkdown { get; set; }

        public static MarkdownParagraphBlockToken Create(IMarkdownRule rule, MarkdownEngine engine, string content)
        {
            return new MarkdownParagraphBlockToken(rule, engine.Context, engine.TokenizeInline(content));
        }

    }
}
