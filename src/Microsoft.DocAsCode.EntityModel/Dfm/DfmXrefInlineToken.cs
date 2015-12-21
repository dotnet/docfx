// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmXrefInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Href { get; }
        public string Name { get; }
        public string Title { get; }
        public string RawMarkdown { get; set; }

        public DfmXrefInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string name, string title)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Name = name;
            Title = title;
        }
    }
}
