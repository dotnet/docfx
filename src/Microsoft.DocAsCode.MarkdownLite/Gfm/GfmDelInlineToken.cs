// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class GfmDelInlineToken : IMarkdownToken, IMarkdownRewritable<GfmDelInlineToken>
    {
        public GfmDelInlineToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> content, string rawMarkdown, LineInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }

        public GfmDelInlineToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new GfmDelInlineToken(Rule, Context, tokens, RawMarkdown, LineInfo);
        }
    }
}
