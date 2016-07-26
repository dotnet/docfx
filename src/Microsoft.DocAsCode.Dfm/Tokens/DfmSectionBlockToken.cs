// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmSectionBlockToken : IMarkdownToken, IDfmBlockSpecialSplitToken
    {
        public DfmSectionBlockToken(IMarkdownRule rule, IMarkdownContext context, string attributes, SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Attributes = attributes;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Attributes { get; }

        public SourceInfo SourceInfo { get; }
    }
}
