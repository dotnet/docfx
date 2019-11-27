// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;

    using Markdig.Helpers;
    using Markdig.Parsers;

    public class CodeSnippetParser : BlockParser
    {
        private const string StartString = "[!code";
        private const string NotebookStartString = "[!notebook";

        public CodeSnippetParser()
        {
            OpeningCharacters = new[] { '[' };
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

            var codeSnippet = new CodeSnippet(this);
            codeSnippet.IsNotebookCode = isNotebookCode;
            MatchLanguage(processor, ref slice, ref codeSnippet);

            if (!MatchName(processor, ref slice, ref codeSnippet))
            {
                return BlockState.None;
            }

            if (!MatchPath(processor, ref slice, ref codeSnippet))
            {
                return BlockState.None;
            }

            MatchQuery(processor, ref slice, ref codeSnippet);

            MatchTitle(processor, ref slice, ref codeSnippet);

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

        private bool MatchStart(ref StringSlice slice)
        {
            var pc = slice.PeekCharExtra(-1);
            if (pc == '\\') return false;

            var c = slice.CurrentChar;
            var index = 0;

            while (c != '\0' && index < StartString.Length && Char.ToLower(c) == StartString[index])
            {
                c = slice.NextChar();
                index++;
            }

            return index == StartString.Length;
        }

        private bool MatchLanguage(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

        private bool MatchPath(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

        private bool MatchName(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

        private bool MatchQuery(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
        {
            var questionMarkMatched = MatchQuestionMarkQuery(processor, ref slice, ref codeSnippet);

            var bookMarkMatched = MatchBookMarkQuery(processor, ref slice, ref codeSnippet);

            return questionMarkMatched || bookMarkMatched;
        }

        private bool MatchQuestionMarkQuery(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

        private bool MatchBookMarkQuery(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

            CodeRange codeRange;
            if (TryGetLineRange(queryString, out codeRange))
            {
                codeSnippet.BookMarkRange = codeRange;
            }
            else
            {
                codeSnippet.TagName = queryString;
            }

            return true;
        }

        private bool MatchTitle(BlockProcessor processor, ref StringSlice slice, ref CodeSnippet codeSnippet)
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

        private bool TryParseQuery(string queryString, ref CodeSnippet codeSnippet)
        {
            if (string.IsNullOrEmpty(queryString)) return false;

            var splitQueryItems = queryString.Split(new[] { '&' });

            int start = -1, end = -1;

            foreach (var queryItem in splitQueryItems)
            {
                var keyValueSplit = queryItem.Split(new[] { '=' });
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
                        if (!TryGetLineRanges(value, out temp))
                        {
                            return false;
                        }

                        codeSnippet.CodeRanges = temp;
                        break;
                    case "highlight":
                        if (!TryGetLineRanges(value, out temp))
                        {
                            return false;
                        }

                        codeSnippet.HighlightRanges = temp;
                        break;
                    case "dedent":
                        int dedent;

                        if (!int.TryParse(value, out dedent))
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

        private bool TryGetLineRanges(string query, out List<CodeRange> codeRanges)
        {
            codeRanges = null;
            if (string.IsNullOrEmpty(query)) return false;

            var rangesSplit = query.Split(new[] { ',' });

            foreach (var range in rangesSplit)
            {
                CodeRange codeRange;
                if (!TryGetLineRange(range, out codeRange, false))
                {
                    return false;
                }

                if (codeRanges == null)
                {
                    codeRanges = new List<CodeRange>();
                }

                codeRanges.Add(codeRange);
            }

            return true;
        }

        private bool TryGetLineRange(string query, out CodeRange codeRange, bool withL = true)
        {
            codeRange = null;
            if (string.IsNullOrEmpty(query)) return false;

            int startLine, endLine;

            var splitLine = query.Split(new[] { '-' });
            if (splitLine.Length > 2) return false;

            var result = TryGetLineNumber(splitLine[0], out startLine, withL);
            endLine = startLine;

            if (splitLine.Length > 1)
            {
                result &= TryGetLineNumber(splitLine[1], out endLine, withL);
            }

            codeRange = new CodeRange { Start = startLine, End = endLine };

            return result;
        }

        private bool TryGetLineNumber(string lineNumberString, out int lineNumber, bool withL = true)
        {
            lineNumber = int.MaxValue;
            if (string.IsNullOrEmpty(lineNumberString)) return true;

            if (withL && (lineNumberString.Length < 2 || Char.ToUpper(lineNumberString[0]) != 'L')) return false;

            return int.TryParse(withL ? lineNumberString.Substring(1) : lineNumberString, out lineNumber);

        }
    }
}
