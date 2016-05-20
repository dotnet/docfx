// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownImageInlineToken : IMarkdownToken
    {
        public MarkdownImageInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string title, string text, string rawMarkdown, LineInfo lineInfo = default(LineInfo))
        {
            Rule = rule;
            Context = context;
            Href = href;
            Title = title;
            Text = text;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Href { get; }

        public string Title { get; }

        public string Text { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }
    }
}
