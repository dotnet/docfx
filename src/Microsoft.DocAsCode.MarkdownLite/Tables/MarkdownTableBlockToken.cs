// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownTableBlockToken : IMarkdownToken
    {
        public MarkdownTableBlockToken(
            IMarkdownRule rule,
            IMarkdownContext context,
            ImmutableArray<InlineContent> header,
            ImmutableArray<Align> align,
            ImmutableArray<ImmutableArray<InlineContent>> cells)
        {
            Rule = rule;
            Context = context;
            Header = header;
            Align = align;
            Cells = cells;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<InlineContent> Header { get; }

        public ImmutableArray<Align> Align { get; }

        public ImmutableArray<ImmutableArray<InlineContent>> Cells { get; }

        public string RawMarkdown { get; set; }
    }
}
