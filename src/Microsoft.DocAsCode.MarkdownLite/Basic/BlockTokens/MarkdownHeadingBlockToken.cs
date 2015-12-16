// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class MarkdownHeadingBlockToken : IMarkdownToken
    {
        public MarkdownHeadingBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> content, string id, int depth)
        {
            Rule = rule;
            Context = context;
            Content = content;
            Id = id;
            Depth = depth;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<IMarkdownToken> Content { get; }

        public string Id { get; }

        public int Depth { get; }

        public string RawMarkdown { get; set; }
    }
}
