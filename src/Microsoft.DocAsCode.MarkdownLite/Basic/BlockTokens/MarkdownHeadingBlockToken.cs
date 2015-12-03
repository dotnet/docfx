// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownHeadingBlockToken : IMarkdownToken
    {
        public MarkdownHeadingBlockToken(IMarkdownRule rule, string content, int depth)
        {
            Rule = rule;
            Content = content;
            Depth = depth;
        }

        public IMarkdownRule Rule { get; }

        public string Content { get; }

        public int Depth { get; }

        public string RawMarkdown { get; set; }
    }
}
