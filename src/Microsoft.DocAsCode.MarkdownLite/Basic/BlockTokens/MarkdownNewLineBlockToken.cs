// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownNewLineBlockToken : IMarkdownToken
    {
        public MarkdownNewLineBlockToken(IMarkdownRule rule, IMarkdownContext context)
        {
            Rule = rule;
            Context = context;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string RawMarkdown { get; set; }
    }
}
