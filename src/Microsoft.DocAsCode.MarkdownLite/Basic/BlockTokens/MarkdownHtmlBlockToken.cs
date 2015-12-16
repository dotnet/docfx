// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownHtmlBlockToken : IMarkdownToken
    {
        public MarkdownHtmlBlockToken(IMarkdownRule rule, IMarkdownContext context, InlineContent content)
        {
            Rule = rule;
            Context = context;
            Content = content;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public InlineContent Content { get; }

        public string RawMarkdown { get; set; }
    }
}
