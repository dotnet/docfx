// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownHtmlBlockToken : IMarkdownToken, IMarkdownRewritable<MarkdownHtmlBlockToken>
    {
        public MarkdownHtmlBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content, string rawMarkdown, LineInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }

        public MarkdownHtmlBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var c = Content.Rewrite(rewriterEngine);
            if (c == Content)
            {
                return this;
            }
            return new MarkdownHtmlBlockToken(Rule, Context, c, RawMarkdown, LineInfo);
        }
    }
}
