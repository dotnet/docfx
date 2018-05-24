// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;

    public class RenderZoneParser : BlockParser
    {
        private const string StartString = "zone";
        private const string EndString = "zone-end";
        private const char Colon = ':';

        private readonly MarkdownContext _context;

        public RenderZoneParser(MarkdownContext context)
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
            if (ExtensionsHelper.IsEscaped(slice))
            {
                return BlockState.None;
            }

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

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, "render=\"", false))
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
                _context.LogWarning("invalid-render-zone", "Zone render does not have ending character (\").");
                return BlockState.None;
            }

            c = slice.NextChar();
            while (c.IsSpace())
            {
                c = slice.NextChar();
            }

            if (!c.IsZero())
            {
                _context.LogWarning("invalid-render-zone", $"Zone render has some invalid chars in the beginning.");
            }

            // Check the blockprocessor context to see if we are already inside of a zone
            // container. If so, break.
            var containerBlock = processor.CurrentContainer;
            do
            {
                if (processor.CurrentContainer.GetType() == typeof(RenderZoneBlock))
                {
                    _context.LogError("invalid-render-zone", "Zone render cannot be nested.");
                    return BlockState.None;
                }
                containerBlock = containerBlock.Parent;
            } while (containerBlock != null);

            processor.NewBlocks.Push(new RenderZoneBlock(this)
            {
                Closed = false,
                ColonCount = colonCount,
                Column = column,
                Span = new SourceSpan(sourcePosition, slice.End),
                Target = range.ToString(),
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
            var renderZone = (RenderZoneBlock)block;

            ExtensionsHelper.SkipSpaces(ref slice);

            if(!ExtensionsHelper.MatchStart(ref slice, new string(':', renderZone.ColonCount)))
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
                _context.LogWarning("invalid-render-zone", $"Zone render has some invalid chars in the ending.");
            }

            block.UpdateSpanEnd(slice.End);
            renderZone.Closed = true;

            return BlockState.BreakDiscard;
        }

        public override bool Close(BlockProcessor processor, Block block)
        {
            if (processor.CurrentContainer != block)
            {
                _context.LogError("invalid-render-zone", "Invalid stack order. A render zone cannot end before other nested blocks have ended.");
                return true;
            }
            var renderZone = (RenderZoneBlock)block;
            if (renderZone != null && renderZone.Closed == false)
            {
                _context.LogWarning("invalid-render-zone", $"No \"::: {EndString}\" found for \"{renderZone.Target}\", zone does not end explictly.");
            }
            return true;
        }
    }
}
