// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTagInlineToken : IMarkdownToken
    {
        public MarkdownTagInlineToken(IMarkdownRule rule, IMarkdownContext context, SourceInfo lineInfo)
        {
            Rule = rule;
            Context = context;
            SourceInfo = lineInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public SourceInfo SourceInfo { get; }
    }
}
