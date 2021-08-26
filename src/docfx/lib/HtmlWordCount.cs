// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using HtmlReaderWriter;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// This utility is shared with:
    /// - Docs.Localization.Build
    /// When updating on one side, please remember to sync the changes.
    /// </summary>
    internal static class HtmlWordCount
    {
        public static void CountWord(ref HtmlToken token, ref long wordCount)
        {
            if (token.Type == HtmlTokenType.Text)
            {
                wordCount += CountWordInText(token.RawText.Span);
            }
        }

        private static int CountWordInText(ReadOnlySpan<char> text)
        {
            var total = 0;
            var word = false;

            foreach (var ch in text)
            {
                if (IsCJKChar(ch))
                {
                    total++;

                    if (word)
                    {
                        word = false;
                        total++;
                    }
                }
                else
                {
                    if (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                    {
                        if (word)
                        {
                            word = false;
                            total++;
                        }
                    }
                    else if (
                        ch != '.' && ch != '?' && ch != '!' &&
                        ch != ';' && ch != ':' && ch != ',' &&
                        ch != '(' && ch != ')' && ch != '[' &&
                        ch != ']')
                    {
                        word = true;
                    }
                }
            }

            if (word)
            {
                total++;
            }

            return total;
        }

        private static bool IsCJKChar(char ch)
        {
            return (ch >= '\u2E80' && ch <= '\u9FFF') || // CJK character
                   (ch >= '\xAC00' && ch <= '\xD7A3') || // Hangul Syllables
                   (ch >= '\uFF00' && ch <= '\uFFEF');   // Half width and Full width Forms (including Chinese punctuation)
        }
    }
}
