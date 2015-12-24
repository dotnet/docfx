// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownBrInlineToken : IMarkdownToken
    {
        public MarkdownBrInlineToken(IMarkdownRule rule, IMarkdownContext context, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; set; }
    }
}
