// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownImageInlineToken : IMarkdownToken
    {
        public MarkdownImageInlineToken(IMarkdownRule rule, IMarkdownContext context, string href, string title, string text, SourceInfo sourceInfo, MarkdownLinkType linkType, string refId)
        {
            Rule = rule;
            Context = context;
            Href = href;
            Title = title;
            Text = text;
            SourceInfo = sourceInfo;
            LinkType = linkType;
            RefId = refId;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public string Href { get; }

        public string Title { get; }

        public string Text { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownLinkType LinkType { get; }

        public string RefId { get; }
    }
}
