// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Markdig.Helpers;
using Markdig.Parsers;

namespace Docfx.MarkdigEngine.Extensions;

public class CodeSnippetParser : BlockParser
{
    private const string StartString = "[!code";
    private const string NotebookStartString = "[!notebook";

    public CodeSnippetParser()
    {
        OpeningCharacters = ['['];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        // Sample: [!code-javascript[Main](../jquery.js?name=testsnippet#tag "title")]
        var slice = processor.Line;
        bool isNotebookCode = false;
        if (!ExtensionsHelper.MatchStart(ref slice, StartString, false))
        {
            slice = processor.Line;
            if (!ExtensionsHelper.MatchStart(ref slice, NotebookStartString, false))
            {
                return BlockState.None;
            }

            isNotebookCode = true;
        }

        var codeSnippet = new CodeSnippet(this)
        {
            IsNotebookCode = isNotebookCode,
        };
        MatchLanguage(ref slice, ref codeSnippet);

        if (!MatchName(ref slice, ref codeSnippet))
        {
            return BlockState.None;
        }

        if (!MatchPath(ref slice, ref codeSnippet))
        {
            return BlockState.None;
        }

        MatchQuery(ref slice, ref codeSnippet);

        MatchTitle(ref slice, ref codeSnippet);

        ExtensionsHelper.SkipWhitespace(ref slice);
        if (slice.CurrentChar == ')')
        {
            slice.NextChar();
            ExtensionsHelper.SkipWhitespace(ref slice);
            if (slice.CurrentChar == ']')
            {
                var codeSnippetEnd = slice.Start;
                slice.NextChar();
                ExtensionsHelper.SkipWhitespace(ref slice);
                if (slice.CurrentChar == '\0')
                {
                    // slice finished its task, re-use it for Raw content
                    slice.Start = processor.Line.Start;
                    slice.End = codeSnippetEnd;
                    codeSnippet.Raw = slice.ToString();
                    codeSnippet.Column = processor.Column;
                    codeSnippet.Line = processor.LineIndex;

                    processor.NewBlocks.Push(codeSnippet);
                    return BlockState.BreakDiscard;
                }
            }
        }

        return BlockState.None;
    }

    private static bool MatchLanguage(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        if (slice.CurrentChar != '-') return false;

        var language = StringBuilderCache.Local();
        var c = slice.NextChar();

        while (c != '\0' && c != '[')
        {
            language.Append(c);
            c = slice.NextChar();
        }

        codeSnippet.Language = language.ToString().Trim();

        return true;
    }

    private static bool MatchPath(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        ExtensionsHelper.SkipWhitespace(ref slice);
        if (slice.CurrentChar != '(') return false;
        var c = slice.NextChar();

        var bracketNeedToMatch = 0;

        var path = StringBuilderCache.Local();
        var hasEscape = false;
        while (c != '\0' && (hasEscape || (c != '#' && c != '?' && c != '"' && (c != ')' || bracketNeedToMatch > 0))))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                if (c == '(' && !hasEscape)
                {
                    bracketNeedToMatch++;
                }
                if (c == ')' && !hasEscape)
                {
                    bracketNeedToMatch--;
                }
                path.Append(c);
                hasEscape = false;
            }
            c = slice.NextChar();
        }

        codeSnippet.CodePath = path.ToString().Trim();

        return true;
    }

    private static bool MatchName(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        if (slice.CurrentChar != '[') return false;

        var c = slice.NextChar();
        var name = StringBuilderCache.Local();
        var hasEscape = false;

        while (c != '\0' && (c != ']' || hasEscape))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                name.Append(c);
                hasEscape = false;
            }
            c = slice.NextChar();
        }

        codeSnippet.Name = name.ToString().Trim();

        if (c == ']')
        {
            slice.NextChar();
            return true;
        }

        return false;
    }

    private static bool MatchQuery(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        var questionMarkMatched = MatchQuestionMarkQuery(ref slice, ref codeSnippet);

        var bookMarkMatched = MatchBookMarkQuery(ref slice, ref codeSnippet);

        return questionMarkMatched || bookMarkMatched;
    }

    private static bool MatchQuestionMarkQuery(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        if (slice.CurrentChar != '?') return false;

        var queryChar = slice.CurrentChar;
        var query = StringBuilderCache.Local();
        var c = slice.NextChar();

        while (c != '\0' && c != '"' && c != ')' && c != '#')
        {
            query.Append(c);
            c = slice.NextChar();
        }

        var queryString = query.ToString().Trim();

        return TryParseQuery(queryString, ref codeSnippet);
    }

    private static bool MatchBookMarkQuery(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        if (slice.CurrentChar != '#') return false;

        var queryChar = slice.CurrentChar;
        var query = StringBuilderCache.Local();
        var c = slice.NextChar();

        while (c != '\0' && c != '"' && c != ')')
        {
            query.Append(c);
            c = slice.NextChar();
        }

        var queryString = query.ToString().Trim();

        if (HtmlCodeSnippetRenderer.TryGetLineRange(queryString, out var codeRange))
        {
            codeSnippet.BookMarkRange = codeRange;
        }
        else
        {
            codeSnippet.TagName = queryString;
        }

        return true;
    }

    private static bool MatchTitle(ref StringSlice slice, ref CodeSnippet codeSnippet)
    {
        if (slice.CurrentChar != '"') return false;

        var title = StringBuilderCache.Local();
        var c = slice.NextChar();
        var hasEscape = false;

        while (c != '\0' && (c != '"' || hasEscape))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                title.Append(c);
                hasEscape = false;
            }
            c = slice.NextChar();
        }

        codeSnippet.Title = title.ToString().Trim();

        if (c == '"')
        {
            slice.NextChar();
            return true;
        }

        return false;
    }

    private static bool TryParseQuery(string queryString, ref CodeSnippet codeSnippet)
    {
        if (string.IsNullOrEmpty(queryString)) return false;

        var splitQueryItems = queryString.Split(['&']);

        int start = -1, end = -1;

        foreach (var queryItem in splitQueryItems)
        {
            var keyValueSplit = queryItem.Split(['=']);
            if (keyValueSplit.Length != 2) return false;
            var key = keyValueSplit[0];
            var value = keyValueSplit[1];

            List<CodeRange> temp;
            switch (key.ToLower())
            {
                case "name":
                    codeSnippet.TagName = value;
                    break;
                case "start":
                    if (!int.TryParse(value, out start))
                    {
                        return false;
                    }
                    end = start;
                    break;
                case "end":
                    if (!int.TryParse(value, out end))
                    {
                        return false;
                    }
                    break;
                case "range":
                    if (!HtmlCodeSnippetRenderer.TryGetLineRanges(value, out temp))
                    {
                        return false;
                    }

                    codeSnippet.CodeRanges = temp;
                    break;
                case "highlight":
                    if (!HtmlCodeSnippetRenderer.TryGetLineRanges(value, out temp))
                    {
                        return false;
                    }

                    codeSnippet.HighlightRanges = temp;
                    break;
                case "dedent":

                    if (!int.TryParse(value, out var dedent))
                    {
                        return false;
                    }

                    codeSnippet.DedentLength = dedent;
                    break;
                default:
                    return false;
            }

        }

        if (start != -1 && end != -1)
        {
            codeSnippet.StartEndRange = new CodeRange { Start = start, End = end };
        }

        return true;
    }

}
