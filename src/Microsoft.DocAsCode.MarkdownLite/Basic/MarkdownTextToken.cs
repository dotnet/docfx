// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTextToken : IMarkdownToken
    {
        public MarkdownTextToken(IMarkdownRule rule, IMarkdownContext context, string content, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Content = content;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Content { get; }

        public string RawMarkdown { get; set; }
    }
}
