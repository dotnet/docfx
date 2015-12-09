// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Generic;

    using MarkdownLite;

    public class DfmSectionBeginBlockToken : IMarkdownToken
    {
        public DfmSectionBeginBlockToken(IMarkdownRule rule, string attributes)
        {
            Rule = rule;
            Attributes = attributes;
        }

        public IMarkdownRule Rule { get; }

        public string Attributes { get; }

        public string RawMarkdown { get; set; }
    }
}
