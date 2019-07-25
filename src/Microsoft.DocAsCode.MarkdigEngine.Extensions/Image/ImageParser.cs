// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Syntax;
    using System;
    using System.Linq;
    using System.Text;

    public class ImageParser : BlockParser
    {
        private const string StartString = "image";
        private const string EndString = "image-end:::";
        private const char Colon = ':';

        private readonly MarkdownContext _context;

        public ImageParser(MarkdownContext context)
        {
            OpeningCharacters = new[] { ':' };
            _context = context;
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsBlankLine)
            {
                return BlockState.Continue;
            }

            var slice = processor.Line;
            var column = processor.Column;
            var sourcePosition = processor.Start;
            var colonCount = 0;
            ExtensionsHelper.SkipSpaces(ref slice);

            var src = StringBuilderCache.Local();
            var alt = new StringBuilder();
            var id = new StringBuilder();

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

            while (slice.CurrentChar != ':')
            {
                ExtensionsHelper.SkipSpaces(ref slice);

                if (ExtensionsHelper.MatchStart(ref slice, "source=\"", false))
                {
                    c = slice.CurrentChar;

                    while (c != '"')
                    {
                        src.Append(c);
                        c = slice.NextChar();
                    }

                    if (!ExtensionsHelper.MatchStart(ref slice, "\"", false))
                    {
                        return BlockState.None;
                    }
                }
                else if (ExtensionsHelper.MatchStart(ref slice, "alt-text=\"", false))
                {
                    c = slice.CurrentChar;

                    while (c != '"')
                    {
                        alt.Append(c);
                        c = slice.NextChar();
                    }

                    if (!ExtensionsHelper.MatchStart(ref slice, "\"", false))
                    {
                        return BlockState.None;
                    }
                }
                else if (ExtensionsHelper.MatchStart(ref slice, "id=\"", false))
                {
                    c = slice.CurrentChar;

                    while (c != '"')
                    {
                        id.Append(c);
                        c = slice.NextChar();
                    }

                    if (!ExtensionsHelper.MatchStart(ref slice, "\"", false))
                    {
                        return BlockState.None;
                    }
                }
                else
                {
                    if (slice.CurrentChar != ':')
                    {
                        c = slice.NextChar();
                    }
                }

            };

            var idExplicitySet = id.ToString();
            if (string.IsNullOrEmpty(idExplicitySet))
            {
                idExplicitySet = src.ToString();
                if (idExplicitySet.IndexOf('/') > -1)
                {
                    idExplicitySet = idExplicitySet.Split('/').Last();
                }
                if (idExplicitySet.IndexOf('.') > -1)
                {
                    Random random = new Random();
                    const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
                    var generated = new string(Enumerable.Repeat(chars, 8)
                      .Select(s => s[random.Next(s.Length)]).ToArray());
                    idExplicitySet = $"{idExplicitySet.Split('.')[0]}-{generated}";
                }
            }
            while (c.IsSpace())
            {
                c = slice.NextChar();
            }

            if (!ExtensionsHelper.MatchStart(ref slice, ":::", false))
            {
                return BlockState.None;
            }

            processor.NewBlocks.Push(new ImageBlock(this)
            {
                Line = processor.LineIndex,
                Src = src.ToString(),
                Alt = alt.ToString(),
                Id = idExplicitySet,
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
            var NestedColumn = (ImageBlock)block;

            ExtensionsHelper.SkipSpaces(ref slice);

            if (!ExtensionsHelper.MatchStart(ref slice, new string(':', NestedColumn.ColonCount)))
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
                _context.LogWarning("invalid-nested-column", $"NestedColumn have some invalid chars in the ending.", block);
            }

            block.UpdateSpanEnd(slice.End);

            return BlockState.BreakDiscard;
        }
    }
}
