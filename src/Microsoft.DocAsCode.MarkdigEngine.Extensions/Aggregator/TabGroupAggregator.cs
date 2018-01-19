// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Microsoft.DocAsCode.Common;

    public class TabGroupAggregator : BlockAggregator<HeadingBlock>
    {
        private static readonly Regex HrefRegex = new Regex(@"^#tab\/(?<id>[a-zA-Z0-9\-]+(?:\+[a-zA-Z0-9\-]+)*)(?:\/(?<condition>[a-zA-Z0-9\-]+)?)?$", RegexOptions.Compiled);

        protected override bool AggregateCore(HeadingBlock headBlock, BlockAggregateContext context)
        {
            var pair = ParseHeading(headBlock);
            if (pair == null)
            {
                return false;
            }
            int offset = 1;
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
                    case ThematicBreakBlock hr:
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
            int offset
            )
        {
            var groupId = (items[0]?.Content?.ToString() ?? string.Empty).GetMd5String().Replace("/", "-").Remove(10);

            context.AggregateTo(new TabGroupBlock(
                                groupId,
                                items.ToImmutableArray(),
                                startLine,
                                startSpan,
                                0),
                                offset);
        }

        private static TabItemBlock CreateTabItem(
            HeadingBlock headBlock,
            Tuple<string, string, LinkInline> pair,
            List<Block> blocks,
            ref int offset)
        {
            foreach (var block in blocks)
            {
                block.Parent.Remove(block);
            }
            offset -= blocks.Count;

            var title = new TabTitleBlock
            {
                Inline = pair.Item3,
                Line = pair.Item3.Line,
                Span = pair.Item3.Span
            };
            var content = new TabContentBlock(blocks);

            return new TabItemBlock(
                pair.Item1,
                pair.Item2,
                title,
                content,
                true);
        }

        private static Tuple<string, string, LinkInline> ParseHeading(HeadingBlock block)
        {
            var inlines = block.Inline.ToList();
            if (inlines.Count == 1 && inlines.First() is LinkInline link)
            {
                var m = HrefRegex.Match(link.Url);
                if (m.Success)
                {
                    return Tuple.Create(m.Groups["id"].Value, m.Groups["condition"].Value, link);
                }
            }

            return null;
        }
    }
}
