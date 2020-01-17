// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class RowParser : BlockParser
    {
        private const string StartString = "row:::";
        private const string EndString = "row-end:::";
        private const char Colon = ':';

        private readonly MarkdownContext _context;

        public RowParser(MarkdownContext context)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var slice = processor.Line;
            var column = processor.Column;
            var sourcePosition = processor.Start;
            var colonCount = 0;

            var c = slice.CurrentChar;
            while (c == Colon)
            {
                c = slice.NextChar();
                colonCount++;
            }

            if (colonCount < 3) return BlockState.None;

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
            {
                return BlockState.None;
            }

            processor.NewBlocks.Push(new RowBlock(this)
            {
                Line = processor.LineIndex,
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
            var Row = (RowBlock)block;

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, new string(':', Row.ColonCount)))
            {
                return BlockState.Continue;
            }

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, EndString, false))
            {
                return BlockState.Continue;
            }

            var c = ExtensionsHelper.SkipSpaces(ref slice);

            if (!c.IsZero())
            {
                _context.LogWarning("invalid-row", "Row has some invalid chars in the ending.", block);
            }

            block.UpdateSpanEnd(slice.End);

            return BlockState.BreakDiscard;
        }
    }
}