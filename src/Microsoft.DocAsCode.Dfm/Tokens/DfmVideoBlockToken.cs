// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmVideoBlockToken : IMarkdownToken, IDfmBlockSpecialSplitToken
    {
        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Link { get; }

        public SourceInfo SourceInfo { get; }

        public DfmVideoBlockToken(IMarkdownRule rule, IMarkdownContext context, string link, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Link = link;
            SourceInfo = sourceInfo;
        }
    }
}
