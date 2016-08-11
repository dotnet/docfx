// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public class MarkdownLinkInlineToken : IMarkdownExpression, IMarkdownRewritable<MarkdownLinkInlineToken>
    {
        public MarkdownLinkInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string title, ImmutableArray<IMarkdownToken> content, SourceInfo sourceInfo, MarkdownLinkType linkType, string refId)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Title = title;
            Content = content;
            SourceInfo = sourceInfo;
            LinkType = linkType;
            RefId = refId;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Href { get; }

        public string Title { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownLinkType LinkType { get; }

        public string RefId { get; }

        public MarkdownLinkInlineToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var tokens = rewriterEngine.Rewrite(Content);
            if (tokens == Content)
            {
                return this;
            }
            return new MarkdownLinkInlineToken(Rule, Context, Href, Title, tokens, SourceInfo, LinkType, RefId);
        }

        public IEnumerable<IMarkdownToken> GetChildren() => Content;
    }
}
