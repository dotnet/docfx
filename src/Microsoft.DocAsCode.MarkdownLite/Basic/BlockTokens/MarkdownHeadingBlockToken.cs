// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownHeadingBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownHeadingBlockToken>
    {
        public MarkdownHeadingBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content, string id, int depth, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Content = content;
            Id = id;
            Depth = depth;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public string Id { get; }

        public int Depth { get; }

        public string RawMarkdown { get; set; }

        public MarkdownHeadingBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var c = Content.Rewrite(rewriterEngine);
            if (c == Content)
            {
                return this;
            }
            return new MarkdownHeadingBlockToken(Rule, Context, c, Id, Depth, RawMarkdown);
        }
    }
}
