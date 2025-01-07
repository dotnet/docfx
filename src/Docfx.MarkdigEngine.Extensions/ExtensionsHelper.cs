// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

using Markdig.Helpers;
using Markdig.Parsers;

namespace Docfx.MarkdigEngine.Extensions;

public static partial class ExtensionsHelper
{
    [GeneratedRegex("&")]
    private static partial Regex HtmlEscapeWithEncode();

    [GeneratedRegex(@"&(?!#?\w+;)")]
    private static partial Regex HtmlEscapeWithoutEncode();

    [GeneratedRegex(@"&([#\w]+);")]
    private static partial Regex HtmlUnescape();

    public static char SkipSpaces(ref StringSlice slice)
    {
        var c = slice.CurrentChar;

        while (c.IsSpaceOrTab())
        {
            c = slice.NextChar();
        }

        return c;
    }

    public static string Escape(string html, bool encode = false)
    {
        return html
            .ReplaceRegex(encode ? HtmlEscapeWithEncode() : HtmlEscapeWithoutEncode(), "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    public static string Unescape(string html)
    {
        return HtmlUnescape().Replace(html, match =>
        {
            var n = match.Groups[1].Value;

            n = n.ToLower();
            if (n == "amp") return "&";
            if (n == "colon") return ":";
            if (n[0] == '#')
            {
                return n[1] == 'x'
                    ? ((char)Convert.ToInt32(n.Substring(2), 16)).ToString()
                    : ((char)Convert.ToInt32(n.Substring(1))).ToString();
            }
            return string.Empty;
        });
    }

    public static string ReplaceRegex(this string input, Regex pattern, string replacement)
    {
        return pattern.Replace(input, replacement);
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return Path.GetFullPath(path).Replace('\\', '/');
    }

    public static bool MatchStart(ref StringSlice slice, string startString, bool isCaseSensitive = true)
    {
        var c = slice.CurrentChar;
        var index = 0;

        while (c != '\0' && index < startString.Length && CharEqual(c, startString[index], isCaseSensitive))
        {
            c = slice.NextChar();
            index++;
        }

        return index == startString.Length;
    }

    public static void ResetLineIndent(BlockProcessor processor)
    {
        processor.GoToColumn(processor.ColumnBeforeIndent);
    }

    public static bool MatchStart(BlockProcessor processor, string startString, bool isCaseSensitive = true)
    {
        var c = processor.CurrentChar;
        var index = 0;

        while (c != '\0' && index < startString.Length && CharEqual(c, startString[index], isCaseSensitive))
        {
            c = processor.NextChar();
            index++;
        }

        return index == startString.Length;
    }

    public static bool MatchLink(ref StringSlice slice, ref string title, ref string path)
    {
        if (MatchTitle(ref slice, ref title) && MatchPath(ref slice, ref path))
        {
            return true;
        }

        return false;
    }

    public static bool MatchInclusionEnd(ref StringSlice slice)
    {
        if (slice.CurrentChar != ']')
        {
            return false;
        }

        slice.NextChar();

        return true;
    }

    public static void SkipWhitespace(ref StringSlice slice)
    {
        var c = slice.CurrentChar;
        while (c != '\0' && c.IsWhitespace())
        {
            c = slice.NextChar();
        }
    }

    public static string TryGetStringBeforeChars(IReadOnlyList<char> chars, ref StringSlice slice, bool breakOnWhitespace = false)
    {
        StringSlice savedSlice = slice;
        var c = slice.CurrentChar;
        var hasEscape = false;
        var builder = StringBuilderCache.Local();

        while (c != '\0' && (!breakOnWhitespace || !c.IsWhitespace()) && (hasEscape || !chars.Contains(c)))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                builder.Append(c);
                hasEscape = false;
            }
            c = slice.NextChar();
        }

        if (c == '\0' && !chars.Contains('\0'))
        {
            slice = savedSlice;
            builder.Length = 0;
            return null;
        }
        else
        {
            var result = builder.ToString().Trim();
            builder.Length = 0;
            return result;
        }
    }

    #region private methods
    private static bool CharEqual(char ch1, char ch2, bool isCaseSensitive)
    {
        return isCaseSensitive ? ch1 == ch2 : char.ToLower(ch1) == char.ToLower(ch2);
    }

    private static bool MatchTitle(ref StringSlice slice, ref string title)
    {
        while (slice.CurrentChar == ' ')
        {
            slice.NextChar();
        }

        if (slice.CurrentChar != '[')
        {
            return false;
        }

        var c = slice.NextChar();
        var str = StringBuilderCache.Local();
        var hasEscape = false;

        while (c != '\0' && (c != ']' || hasEscape))
        {
            if (c == '\\' && !hasEscape)
            {
                hasEscape = true;
            }
            else
            {
                str.Append(c);
                hasEscape = false;
            }
            c = slice.NextChar();
        }

        if (c == ']')
        {
            title = str.ToString().Trim();
            slice.NextChar();

            return true;
        }

        return false;
    }

    public static bool IsEscaped(StringSlice slice)
    {
        return slice.PeekCharExtra(-1) == '\\';
    }

    private static bool MatchPath(ref StringSlice slice, ref string path)
    {
        if (slice.CurrentChar != '(')
        {
            return false;
        }

        slice.NextChar();
        SkipWhitespace(ref slice);

        string includedFilePath;
        if (slice.CurrentChar == '<')
        {
            includedFilePath = TryGetStringBeforeChars([')', '>'], ref slice, breakOnWhitespace: true);
        }
        else
        {
            includedFilePath = TryGetStringBeforeChars([')'], ref slice, breakOnWhitespace: true);
        }

        if (includedFilePath == null)
        {
            return false;
        }

        if (includedFilePath.Length >= 1 && includedFilePath.First() == '<' && slice.CurrentChar == '>')
        {
            includedFilePath = includedFilePath.Substring(1, includedFilePath.Length - 1).Trim();
        }

        if (slice.CurrentChar == ')')
        {
            path = includedFilePath;
            slice.NextChar();
            return true;
        }
        else
        {
            var title = TryGetStringBeforeChars([')'], ref slice, breakOnWhitespace: false);
            if (title == null)
            {
                return false;
            }
            else
            {
                path = includedFilePath;
                slice.NextChar();
                return true;
            }
        }
    }
    #endregion
}
