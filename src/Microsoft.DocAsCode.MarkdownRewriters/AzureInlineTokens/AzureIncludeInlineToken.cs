// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureIncludeInlineToken : IMarkdownToken
    {
        public IMarkdownRule Rule { get; }
        public IMarkdownContext Context { get; }
        public string Src { get; }
        public string Name { get; }
        public string Title { get; }
        public string Raw { get; }
        public string RawMarkdown { get; set; }

        public AzureIncludeInlineToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, string raw, string rawMarkdown)
        {
            Rule = rule;
            Context = context;
            Src = src;
            Name = name;
            Title = title;
            Raw = raw;
            RawMarkdown = rawMarkdown;
        }
    }
}
