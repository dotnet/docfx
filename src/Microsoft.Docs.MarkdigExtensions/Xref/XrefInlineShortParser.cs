// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Microsoft.Docs.MarkdigExtensions;

internal class XrefInlineShortParser : InlineParser
{
    private const string ContinuableCharacters = ".,;:!?~";
    private const string StopCharacters = @"""'<>[]|";

    public XrefInlineShortParser()
    {
        OpeningCharacters = new[] { '@' };
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        var c = slice.PeekCharExtra(-1);

        if (!c.IsWhiteSpaceOrZero() && !ContinuableCharacters.Contains(c))
        {
            return false;
        }

        c = slice.NextChar();

        if (c == '\'' || c == '"')
        {
            return MatchXrefShortcutWithQuote(processor, ref slice);
        }
        else
        {
            return MatchXrefShortcut(processor, ref slice);
        }
    }

    private static bool MatchXrefShortcut(InlineProcessor processor, ref StringSlice slice)
    {
        if (!slice.CurrentChar.IsAlpha())
        {
            return false;
        }

        var saved = slice;

        var c = slice.CurrentChar;
        var href = StringBuilderCache.Local();

        while (!c.IsZero())
        {
            // Meet line ends or whitespace
            if (c.IsWhiteSpaceOrZero() || StopCharacters.Contains(c))
            {
                break;
            }

            var nextChar = slice.PeekCharExtra(1);
            if (ContinuableCharacters.Contains(c) && (nextChar.IsWhiteSpaceOrZero() || StopCharacters.Contains(nextChar) || ContinuableCharacters.Contains(nextChar)))
            {
                break;
            }

            href.Append(c);
            c = slice.NextChar();
        }

        var xrefInline = new XrefInline
        {
            Href = href.ToString(),
            Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out var line, out var column), processor.GetSourcePosition(slice.Start - 1)),
            Line = line,
            Column = column,
        };

        var htmlAttributes = xrefInline.GetAttributes();

        var sourceContent = href.Insert(0, '@');
        htmlAttributes.AddPropertyIfNotExist("data-throw-if-not-resolved", "False");
        htmlAttributes.AddPropertyIfNotExist("data-raw-source", sourceContent.ToString());
        processor.Inline = xrefInline;

        return true;
    }

    private static bool MatchXrefShortcutWithQuote(InlineProcessor processor, ref StringSlice slice)
    {
        var saved = slice;

        var startChar = slice.CurrentChar;
        var href = StringBuilderCache.Local();

        var c = slice.NextChar();

        while (c != startChar && !c.IsNewLineOrLineFeed() && !c.IsZero())
        {
            href.Append(c);
            c = slice.NextChar();
        }

        if (c != startChar)
        {
            return false;
        }

        slice.NextChar();

        var xrefInline = new XrefInline
        {
            Href = href.ToString(),
            Span = new SourceSpan(processor.GetSourcePosition(saved.Start, out var line, out var column), processor.GetSourcePosition(slice.Start - 1)),
            Line = line,
            Column = column,
        };

        var htmlAttributes = xrefInline.GetAttributes();

        var sourceContent = href.Insert(0, startChar).Insert(0, '@').Append(startChar);
        htmlAttributes.AddPropertyIfNotExist("data-throw-if-not-resolved", "False");
        htmlAttributes.AddPropertyIfNotExist("data-raw-source", sourceContent.ToString());
        processor.Inline = xrefInline;

        return true;
    }
}
