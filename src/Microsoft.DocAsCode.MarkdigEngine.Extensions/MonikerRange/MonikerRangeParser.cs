// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class MonikerRangeParser : BlockParser
    {
        private const string StartString = "moniker";
        private const string EndString = "moniker-end";
        private const char Colon = ':';

        private readonly MarkdownContext _context;

        public MonikerRangeParser(MarkdownContext context)
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

            if (!ExtensionsHelper.MatchStart(ref slice, "moniker", false))
            {
                return BlockState.None;
            }

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "range=\"", false))
            {
                return BlockState.None;
            }

            var range = StringBuilderCache.Local();
            c = slice.CurrentChar;

            while (c != '\0' && c != '"')
            {
                range.Append(c);
                c = slice.NextChar();
            }

            if (c != '"')
            {
                _context.LogWarning("invalid-moniker-range", "MonikerRange does not have ending charactor (\").");
                return BlockState.None;
            }

            c = slice.NextChar();
            while (c.IsSpace())
            {
                c = slice.NextChar();
            }

            if (!c.IsZero())
            {
                _context.LogWarning("invalid-moniker-range", $"MonikerRange have some invalid chars in the starting.");
            }

            processor.NewBlocks.Push(new MonikerRangeBlock(this)
            {
                Closed = false,
                MonikerRange = range.ToString(),
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
            var monikerRange = (MonikerRangeBlock)block;

            ExtensionsHelper.SkipSpaces(ref slice);

            if(!ExtensionsHelper.MatchStart(ref slice, new string(':', monikerRange.ColonCount)))
            {
                ExtensionsHelper.ResetLineIndent(processor);
                return BlockState.Continue;
            }

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "moniker-end", false))
            {
                ExtensionsHelper.ResetLineIndent(processor);
                return BlockState.Continue;
            }

            var c = ExtensionsHelper.SkipSpaces(ref slice);

            if (!c.IsZero())
            {
                _context.LogWarning("invalid-moniker-range", $"MonikerRange have some invalid chars in the ending.");
            }

            block.UpdateSpanEnd(slice.End);
            monikerRange.Closed = true;

            return BlockState.BreakDiscard;
        }

        public override bool Close(BlockProcessor processor, Block block)
        {
            var monikerRange = (MonikerRangeBlock)block;
            if (monikerRange != null && monikerRange.Closed == false)
            {
                _context.LogWarning("invalid-moniker-range", $"No \"::: {EndString}\" found for \"{monikerRange.MonikerRange}\", MonikerRange does not end explictly.");
            }
            return true;
        }
    }
}
