// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTagInlineToken : IMarkdownToken
    {
        public MarkdownTagInlineToken(IMarkdownRule rule, IMarkdownContext context, string rawMarkdown, LineInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }
    }
}
