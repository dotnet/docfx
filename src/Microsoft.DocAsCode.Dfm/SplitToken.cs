// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite;

    public class SplitToken : IMarkdownToken
    {
        public IMarkdownToken Token { get; set; }

        public List<IMarkdownToken> InnerTokens { get; set; }

        public SplitToken(IMarkdownToken token)
        {
            Token = token;
            InnerTokens = new List<IMarkdownToken>();
            Rule = token.Rule;
            Context = token.Context;
            SourceInfo = token.SourceInfo;
        }

        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public SourceInfo SourceInfo { get; }
    }
}
