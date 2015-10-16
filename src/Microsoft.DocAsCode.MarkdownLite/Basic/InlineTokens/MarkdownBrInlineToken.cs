// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownBrInlineToken : IMarkdownToken
    {
        public MarkdownBrInlineToken(IMarkdownRule rule)
        {
            Rule = rule;
        }

        public IMarkdownRule Rule { get; }
    }
}
