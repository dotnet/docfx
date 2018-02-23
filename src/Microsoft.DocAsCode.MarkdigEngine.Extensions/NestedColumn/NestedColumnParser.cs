// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Common;

    public class NestedColumnParser : BlockParser
    {
        private const string StartString = "column";
        private const string EndString = "column-end:::";
        private const char Colon = ':';
        public NestedColumnParser()
        {
            OpeningCharacters = new[] { ':' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            var slice = processor.Line;

            if (ExtensionsHelper.IsEscaped(slice))
            {
                return BlockState.None;
            }

            var column = processor.Column;
            var sourcePosition = processor.Start;
            var colonCount = 0;

            SkipSpaces(ref slice);

            var columnWidth = StringBuilderCache.Local();
            var c = slice.CurrentChar;

            while (c == Colon)
            {
                c = slice.NextChar();
                colonCount++;
            }

            if (colonCount < 3) return BlockState.None;

            SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "column", false))
            {
                return BlockState.None;
            }

            SkipSpaces(ref slice);

            if (ExtensionsHelper.MatchStart(ref slice, "span=\"", false))
            {
                c = slice.CurrentChar;

                while (c != '"')
                {
                    columnWidth.Append(c);
                    c = slice.NextChar();
                }

                if (!ExtensionsHelper.MatchStart(ref slice, "\"", false))
                {
                    return BlockState.None;
                }

            } else
            {
                columnWidth.Append("1"); // default span is one
            }

            while (c.IsSpace())
            {
                c = slice.NextChar();
            }

            if (!ExtensionsHelper.MatchStart(ref slice, ":::", false)) //change
            {
                return BlockState.None;
            }
         
                if (!c.IsZero())
            {
                Logger.LogWarning($"NestedColumn have some invalid chars in the starting.");
            }

            processor.NewBlocks.Push(new NestedColumnBlock(this)
            {
                ColumnWidth = columnWidth.ToString(),
                ColonCount = colonCount,
                Column = column,
                Span = new SourceSpan(sourcePosition, slice.End),
            });

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            var slice = processor.Line;
            var NestedColumn = (NestedColumnBlock)block;

            SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, new string(':', NestedColumn.ColonCount)))
            {
                return BlockState.Continue;
            }

            SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "column-end:::", false))
            {
                return BlockState.Continue;
            }

            var c = SkipSpaces(ref slice);

            if (!c.IsZero())
            {
                Logger.LogWarning($"NestedColumn have some invalid chars in the ending.");
            }

            block.UpdateSpanEnd(slice.End);

            return BlockState.BreakDiscard;
        }

        public char SkipSpaces(ref StringSlice slice)
        {
            var c = slice.CurrentChar;

            while (c.IsSpaceOrTab())
            {
                c = slice.NextChar();
            }

            return c;
        }
    }
}
