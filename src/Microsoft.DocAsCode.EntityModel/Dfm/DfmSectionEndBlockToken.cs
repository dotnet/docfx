// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;

    public class DfmSectionEndBlockToken : IMarkdownToken
    {
        public DfmSectionEndBlockToken(IMarkdownRule rule)
        {
            Rule = rule;
        }

        public IMarkdownRule Rule { get; }

        public string RawMarkdown { get; set; }
    }
}
