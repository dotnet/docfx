// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownNpTableBlockRule : IMarkdownRule
    {
        private static readonly Matcher _NpTableMatcher =
            // @" *\|?(.+)\n"
            Matcher.WhiteSpacesOrEmpty + Matcher.Char('|').Maybe() + Matcher.AnyCharNot('\n').RepeatAtLeast(1).ToGroup("header") + Matcher.NewLine +
            // @" *\|? *"
            Matcher.WhiteSpacesOrEmpty + Matcher.Char('|').Maybe() + Matcher.WhiteSpacesOrEmpty +
            // @"([-:]+ *\|[-| :]*)"
            (Matcher.AnyCharIn('-', ':').RepeatAtLeast(1) + Matcher.WhiteSpacesOrEmpty + '|' + Matcher.AnyCharIn('-', '|', ' ', ':').RepeatAtLeast(0)).ToGroup("align") +
            // @"\n"
            Matcher.NewLine +
            // @"((?:.*\|.*(?:\n|$))*)"
            (Matcher.AnyCharNotIn('\n', '|').RepeatAtLeast(0) + '|' + Matcher.AnyCharNot('\n').RepeatAtLeast(0) + (Matcher.NewLine | Matcher.EndOfString)).RepeatAtLeast(0).ToGroup("body") +
            // @"\n*"
            Matcher.NewLine.RepeatAtLeast(0);

        public virtual string Name => "NpTable";

        [Obsolete("Please use NewLineMatcher.")]
        public virtual Regex NpTable => Regexes.Block.Tables.NpTable;

        public virtual Matcher NpTableMatcher => _NpTableMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (NpTable != Regexes.Block.Tables.NpTable || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(NpTableMatcher);
            if (match?.Length > 0)
            {
                var header = match["header"].GetValue().ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter);
                var align = ParseAligns(match["align"].GetValue().ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter));
                if (header.Length > align.Length)
                {
                    return null;
                }
                var sourceInfo = context.Consume(match.Length);

                var rows = (from row in match["body"].GetValue().ReplaceRegex(Regexes.Lexers.EndWithNewLine, string.Empty).Split('\n')
                            select row.ReplaceRegex(Regexes.Lexers.UselessTableRow, string.Empty)).ToList();
                var cells = new string[rows.Count][];
                for (int i = 0; i < rows.Count; i++)
                {
                    var columns = rows[i].SplitRegex(Regexes.Lexers.TableSplitter);
                    if (columns.Length == header.Length)
                    {
                        cells[i] = columns;
                    }
                    else if (columns.Length < header.Length)
                    {
                        cells[i] = new string[header.Length];
                        for (int j = 0; j < columns.Length; j++)
                        {
                            cells[i][j] = columns[j];
                        }
                        for (int j = columns.Length; j < cells[i].Length; j++)
                        {
                            cells[i][j] = string.Empty;
                        }
                    }
                    else // columns.Length > header.Length
                    {
                        cells[i] = new string[header.Length];
                        for (int j = 0; j < header.Length; j++)
                        {
                            cells[i][j] = columns[j];
                        }
                    }
                }

                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    sourceInfo,
                    (p, t) =>
                        new MarkdownTableBlockToken(
                            t.Rule,
                            t.Context,
                            (from text in header
                             let si = t.SourceInfo.Copy(text)
                             select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray(),
                            align.ToImmutableArray(),
                            cells.Select(
                                (row, index) =>
                                    (from col in row
                                     let si = t.SourceInfo.Copy(col, index + 2)
                                     select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray()
                            ).ToImmutableArray(),
                            t.SourceInfo));
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = NpTable.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var header = match.Groups[1].Value.ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter);
            var align = ParseAligns(match.Groups[2].Value.ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter));
            if (header.Length > align.Length)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            var rows = (from row in match.Groups[3].Value.ReplaceRegex(Regexes.Lexers.EndWithNewLine, string.Empty).Split('\n')
                        select row.ReplaceRegex(Regexes.Lexers.UselessTableRow, string.Empty)).ToList();
            var cells = new string[rows.Count][];
            for (int i = 0; i < rows.Count; i++)
            {
                var columns = rows[i].SplitRegex(Regexes.Lexers.TableSplitter);
                if (columns.Length == header.Length)
                {
                    cells[i] = columns;
                }
                else if (columns.Length < header.Length)
                {
                    cells[i] = new string[header.Length];
                    for (int j = 0; j < columns.Length; j++)
                    {
                        cells[i][j] = columns[j];
                    }
                    for (int j = columns.Length; j < cells[i].Length; j++)
                    {
                        cells[i][j] = string.Empty;
                    }
                }
                else // columns.Length > header.Length
                {
                    cells[i] = new string[header.Length];
                    for (int j = 0; j < header.Length; j++)
                    {
                        cells[i][j] = columns[j];
                    }
                }
            }

            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                sourceInfo,
                (p, t) =>
                    new MarkdownTableBlockToken(
                        t.Rule,
                        t.Context,
                        (from text in header
                         let si = t.SourceInfo.Copy(text)
                         select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray(),
                        align.ToImmutableArray(),
                        cells.Select(
                            (row, index) =>
                                (from col in row
                                 let si = t.SourceInfo.Copy(col, index + 2)
                                 select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray()
                        ).ToImmutableArray(),
                        t.SourceInfo));
        }

        protected virtual Align[] ParseAligns(string[] aligns)
        {
            var result = new Align[aligns.Length];
            for (int i = 0; i < aligns.Length; i++)
            {
                if (Regexes.Lexers.TableAlignRight.IsMatch(aligns[i]))
                {
                    result[i] = Align.Right;
                }
                else if (Regexes.Lexers.TableAlignCenter.IsMatch(aligns[i]))
                {
                    result[i] = Align.Center;
                }
                else if (Regexes.Lexers.TableAlignLeft.IsMatch(aligns[i]))
                {
                    result[i] = Align.Left;
                }
                else
                {
                    result[i] = Align.NotSpec;
                }
            }
            return result;
        }
    }
}
