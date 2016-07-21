// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public class MarkdownTableBlockToken : IMarkdownExpression, IMarkdownRewritable<MarkdownTableBlockToken>
    {
        public MarkdownTableBlockToken(
            IMarkdownRule rule,
            IMarkdownContext context,
            ImmutableArray<MarkdownTableItemBlockToken> header,
            ImmutableArray<Align> align,
            ImmutableArray<ImmutableArray<MarkdownTableItemBlockToken>> cells,
            SourceInfo sourceInfo)
        {
            Rule = rule;
            Context = context;
            Header = header;
            Align = align;
            Cells = cells;
            SourceInfo = sourceInfo;
        }

        public IMarkdownRule Rule { get; }

        public IMarkdownContext Context { get; }

        public ImmutableArray<MarkdownTableItemBlockToken> Header { get; }

        public ImmutableArray<Align> Align { get; }

        public ImmutableArray<ImmutableArray<MarkdownTableItemBlockToken>> Cells { get; }

        public SourceInfo SourceInfo { get; }

        public MarkdownTableBlockToken Rewrite(IMarkdownRewriteEngine rewriterEngine)
        {
            var header = Header;
            for (int index = 0; index < header.Length; index++)
            {
                var cell = header[index];
                var rewritten = cell.Rewrite(rewriterEngine);
                if (rewritten != cell)
                {
                    header = header.SetItem(index, rewritten);
                }
            }
            var cells = Cells;
            for (int rowIndex = 0; rowIndex < Cells.Length; rowIndex++)
            {
                var row = cells[rowIndex];
                var rewrittenRow = row;
                for (int columnIndex = 0; columnIndex < row.Length; columnIndex++)
                {
                    var cell = row[columnIndex];
                    var rewritten = cell.Rewrite(rewriterEngine);
                    if (rewritten != cell)
                    {
                        rewrittenRow = rewrittenRow.SetItem(columnIndex, rewritten);
                    }
                }
                if (rewrittenRow != row)
                {
                    cells = cells.SetItem(rowIndex, rewrittenRow);
                }
            }
            if (header == Header && cells == Cells)
            {
                return this;
            }
            return new MarkdownTableBlockToken(Rule, Context, header, Align, cells, SourceInfo);
        }

        public IEnumerable<IMarkdownToken> GetChildren() =>
            (from token in Header select (IMarkdownToken)token)
            .Concat(from row in Cells from cell in row select cell);
    }
}
