// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

public class TabGroupBlock : ContainerBlock
{
    public int Id { get; set; }

    public int ActiveTabIndex { get; set; }

    public ImmutableArray<TabItemBlock> Items { get; set; }

    public TabGroupBlock(ImmutableArray<TabItemBlock> blocks, int startLine, int startSpan, int activeTabIndex)
        : base(null)
    {
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
