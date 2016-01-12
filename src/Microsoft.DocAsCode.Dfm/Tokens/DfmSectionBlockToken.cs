// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmSectionBlockToken : IMarkdownToken
    {
        public DfmSectionBlockToken(IMarkdownRule rule, IMarkdownContext context, string attributes, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Attributes = attributes;
            RawMarkdown = rawMarkdown;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Attributes { get; }

        public string RawMarkdown { get; set; }
    }
}
