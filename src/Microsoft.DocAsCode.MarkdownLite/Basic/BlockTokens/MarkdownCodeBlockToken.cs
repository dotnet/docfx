// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownCodeBlockToken : IMarkdownToken
    {
        public MarkdownCodeBlockToken(IMarkdownRule rule, IMarkdownContext context, string code, string rawMarkdown, string lang, LineInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            Code = code;
            Lang = lang;
            RawMarkdown = rawMarkdown;
            LineInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Code { get; }

        public string Lang { get; }

        public string RawMarkdown { get; }

        public LineInfo LineInfo { get; }
    }
}
