﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class MarkdownTableBlockRule : IMarkdownRule
    {
        public virtual string Name => "Table";

        public virtual Regex Table => Regexes.Block.Tables.Table;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Table.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var header = match.Groups[1].Value.ReplaceRegex(Regexes.Lexers.UselessTableHeader, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter);
            var align = ParseAligns(match.Groups[2].Value.ReplaceRegex(Regexes.Lexers.UselessTableAlign, string.Empty).SplitRegex(Regexes.Lexers.TableSplitter));
            var cells = match.Groups[3].Value.ReplaceRegex(Regexes.Lexers.UselessGfmTableCell, string.Empty).Split('\n').Select(x => new string[] { x }).ToArray();
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = cells[i][0]
                  .ReplaceRegex(Regexes.Lexers.EmptyGfmTableCell, string.Empty)
                  .SplitRegex(Regexes.Lexers.TableSplitter);

                var cellList = cells[i].ToList();
                while (cellList.Count < header.Length)
                {
                    cellList.Add(string.Empty);
                }
                cells[i] = cellList.ToArray();
            }

            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                sourceInfo,
                (p, t) => new MarkdownTableBlockToken(
                    t.Rule,
                    t.Context,
                    (from text in header
                     let si = t.SourceInfo.Copy(text)
                     select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray(),
                    align.ToImmutableArray(),
                    (from row in cells
                     select (from col in row
                             let si = t.SourceInfo.Copy(col)
                             select new MarkdownTableItemBlockToken(t.Rule, t.Context, p.TokenizeInline(si), si)).ToImmutableArray()).ToImmutableArray(),
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
