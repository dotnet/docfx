// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Syntax;

namespace Docfx.MarkdigEngine.Extensions;

public class QuoteSectionNoteParser : BlockParser
{
    private readonly List<string> _noteTypes;
    private readonly MarkdownContext _context;

    public QuoteSectionNoteParser(MarkdownContext context, string[] noteTypes = null)
    {
        OpeningCharacters = ['>'];
        _context = context;
        _noteTypes = noteTypes.Select(s => $"[!{s}]").ToList();
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        var column = processor.Column;
        var sourcePosition = processor.Start;

        var quoteChar = processor.CurrentChar;
        var c = processor.NextChar();
        if (c.IsSpaceOrTab())
        {
            processor.NextColumn();
        }

        var rawNewBlock = new QuoteSectionNoteBlock(this)
        {
            Line = processor.LineIndex,
            QuoteChar = quoteChar,
            Column = column,
            Span = new SourceSpan(sourcePosition, processor.Line.End),
        };
        TryParseFromLine(processor, rawNewBlock);
        processor.NewBlocks.Push(rawNewBlock);

        if (rawNewBlock.QuoteType == QuoteSectionNoteType.DFMVideo)
        {
            return BlockState.BreakDiscard;
        }
        else
        {
            return BlockState.Continue;
        }
    }

    public override BlockState TryContinue(BlockProcessor processor, Block block)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        var quote = (QuoteSectionNoteBlock)block;
        var column = processor.Column;

        if (quote.QuoteType == QuoteSectionNoteType.DFMVideo)
        {
            return BlockState.BreakDiscard;
        }

        var c = processor.CurrentChar;
        if (c != quote.QuoteChar)
        {
            return processor.IsBlankLine ? BlockState.BreakDiscard : BlockState.None;
        }

        c = processor.NextChar(); // Skip opening char
        if (c.IsSpace())
        {
            processor.NextChar(); // Skip following space
        }

        // Check for New DFM block
        if (TryParseFromLine(processor, new QuoteSectionNoteBlock(this)))
        {
            // Meet note or section, close this block, new block will be open in the next steps
            processor.GoToColumn(column);
            return BlockState.None;
        }
        else
        {
            block.UpdateSpanEnd(processor.Line.End);
            return BlockState.Continue;
        }
    }

    private bool TryParseFromLine(BlockProcessor processor, QuoteSectionNoteBlock block)
    {
        int originalColumn = processor.Column;
        block.QuoteType = QuoteSectionNoteType.MarkdownQuote;

        if (processor.CurrentChar != '[')
        {
            return false;
        }

        var stringBuilder = StringBuilderCache.Local();
        var c = processor.CurrentChar;

        var hasEscape = false;
        while (c != '\0' && (c != ']' || hasEscape))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                stringBuilder.Append(c);
                hasEscape = false;
            }
            c = processor.NextChar();
        }

        stringBuilder.Append(c);
        var infoString = stringBuilder.ToString().Trim();

        if (c == '\0')
        {
            processor.GoToColumn(originalColumn);
            return false;
        }

        if (c == ']')
        {
            // "> [!NOTE] content" is invalid, go to end to see it.
            processor.NextChar();
            while (processor.CurrentChar.IsSpaceOrTab())
                processor.NextChar();

            var isNoteVideoDiv = infoString.StartsWith("[!div", StringComparison.OrdinalIgnoreCase) ||
                                 infoString.StartsWith("[!Video", StringComparison.OrdinalIgnoreCase) ||
                                 IsNoteType(infoString);
            if (processor.CurrentChar != '\0' && isNoteVideoDiv)
            {
                _context.LogWarning("invalid-note-section", "Text in the first line of Note/Section/Video is not valid. Will be rendered to <blockquote>", block);
                processor.GoToColumn(originalColumn);
                return false;
            }
        }

        if (IsNoteType(infoString))
        {
            block.QuoteType = QuoteSectionNoteType.DFMNote;
            block.NoteTypeString = infoString.Substring(2, infoString.Length - 3);
            return true;
        }

        if (infoString.StartsWith("[!div", StringComparison.OrdinalIgnoreCase))
        {
            block.QuoteType = QuoteSectionNoteType.DFMSection;
            string attribute = infoString.Substring(5, infoString.Length - 6).Trim();
            if (attribute.Length >= 2 && attribute.First() == '`' && attribute.Last() == '`')
            {
                block.SectionAttributeString = attribute.Substring(1, attribute.Length - 2).Trim();
            }
            if (attribute.Length >= 1 && attribute.First() != '`' && attribute.Last() != '`')
            {
                block.SectionAttributeString = attribute;
            }
            return true;
        }

        if (infoString.StartsWith("[!Video", StringComparison.OrdinalIgnoreCase))
        {
            string link = infoString.Substring(7, infoString.Length - 8);
            if (link.StartsWith(" http://") || link.StartsWith(" https://"))
            {
                block.QuoteType = QuoteSectionNoteType.DFMVideo;
                block.VideoLink = link.Trim();
                return true;
            }
        }

        processor.GoToColumn(originalColumn);
        return false;
    }

    private bool IsNoteType(string infoString)
    {
        foreach (var noteType in _noteTypes)
        {
            if (string.Equals(infoString, noteType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
