// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmXrefInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Href { get; }
        public string Name { get; }
        public string Title { get; }
        public bool ThrowIfNotResolved { get; }
        public string RawMarkdown { get; set; }

        public DfmXrefInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string name, string title, bool throwIfNotResolved, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Name = name;
            Title = title;
            ThrowIfNotResolved = throwIfNotResolved;
            RawMarkdown = rawMarkdown;
        }
    }
}
