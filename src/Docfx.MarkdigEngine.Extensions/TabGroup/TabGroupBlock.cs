// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class TabGroupBlock : ContainerBlock
{
    public int Id { get; set; }

    public int ActiveTabIndex { get; set; }

    public ImmutableArray<TabItemBlock> Items { get; set; }

    public TabGroupBlock(ImmutableArray<TabItemBlock> blocks, int startLine, int startSpan, int activeTabIndex) : base(null)
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
