// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Markdig.Syntax;

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    public class TabGroupBlock : ContainerBlock
    {
        public string Id { get; set; }

        public int ActiveTabIndex { get; set; }

        public ImmutableArray<TabItemBlock> Items { get; set; }

        public TabGroupBlock(string id, ImmutableArray<TabItemBlock> blocks, int startLine, int startSpan, int activeTabIndex)
            : base(null)
        {
            Id = id;
            ActiveTabIndex = activeTabIndex;
            Items = blocks;
            Line = startLine;

            foreach (var item in blocks)
            {
                Add(item.Title);
                Add(item.Content);
            }

            var length = blocks.Length;
            Span = new SourceSpan(startSpan, blocks[length - 1].Content.Span.End);
        }
    }
}