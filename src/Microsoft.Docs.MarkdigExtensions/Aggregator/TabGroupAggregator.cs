// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.MarkdigExtensions;

public class TabGroupAggregator : BlockAggregator<HeadingBlock>
{
    private static readonly Regex s_hrefRegex = new(
        @"^#tab\/(?<id>[a-zA-Z0-9\-]+(?:\+[a-zA-Z0-9\-]+)*)(?:\/(?<condition>[a-zA-Z0-9\-]+)?)?$", RegexOptions.Compiled);

    protected override bool AggregateCore(HeadingBlock headBlock, BlockAggregateContext context)
    {
        var pair = ParseHeading(headBlock);
        if (pair == null)
        {
            return false;
        }
        var offset = 1;
        var items = new List<TabItemBlock>();
        var list = new List<Block>();
        while (true)
        {
            var block = context.LookAhead(offset);
            switch (block)
            {
                case HeadingBlock head:
                    var temp = ParseHeading(head);
                    if (temp == null)
                    {
                        goto default;
                    }
                    items.Add(CreateTabItem(headBlock, pair, list, ref offset));
                    pair = temp;
                    list.Clear();
                    break;
                case ThematicBreakBlock:
                    offset++;
                    list.Add(block);
                    goto case null;
                case null:
                    items.Add(CreateTabItem(headBlock, pair, list, ref offset));
                    var startLine = headBlock.Line;
                    var startSpan = headBlock.Span.Start;
                    AggregateCore(context, items, startLine, startSpan, offset);
                    return true;
                default:
                    list.Add(block);
                    break;
            }
            offset++;
        }
    }

    private static void AggregateCore(
        BlockAggregateContext context,
        List<TabItemBlock> items,
        int startLine,
        int startSpan,
        int offset)
    {
        context.AggregateTo(new TabGroupBlock(items.ToImmutableArray(), startLine, startSpan, 0), offset);
    }

    private static TabItemBlock CreateTabItem(
        HeadingBlock headBlock,
        Tuple<string, string, LinkInline> pair,
        List<Block> blocks,
        ref int offset)
    {
        var parent = headBlock.Parent;
        foreach (var block in blocks)
        {
            parent?.Remove(block);
        }
        offset -= blocks.Count;

        pair.Item3.Remove();
        var title = new TabTitleBlock
        {
            Inline = pair.Item3,
            Line = pair.Item3.Line,
            Span = pair.Item3.Span,
        };
        var content = new TabContentBlock(blocks, string.IsNullOrEmpty(pair.Item2) ? pair.Item1 : pair.Item1 + "/" + pair.Item2);

        return new TabItemBlock(
            pair.Item1,
            pair.Item2,
            title,
            content,
            true);
    }

    private static Tuple<string, string, LinkInline> ParseHeading(HeadingBlock block)
    {
        var child = block.Inline.FirstChild;
        if (child != null && child.NextSibling == null && child is LinkInline link)
        {
            var m = s_hrefRegex.Match(link.Url);
            if (m.Success)
            {
                return Tuple.Create(m.Groups["id"].Value, m.Groups["condition"].Value, link);
            }
        }

        return null;
    }
}
