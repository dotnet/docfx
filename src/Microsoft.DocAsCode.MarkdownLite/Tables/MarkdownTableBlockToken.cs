// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class MarkdownTableBlockToken : IMarkdownToken
    {
        public MarkdownTableBlockToken(IMarkdownRule rule, string[] header, Align[] align, string[][] cells)
        {
            Rule = rule;
            Header = header;
            Align = align;
            Cells = cells;
        }

        public IMarkdownRule Rule { get; }

        public string[] Header { get; }

        public Align[] Align { get; }

        public string[][] Cells { get; }

        public string SourceMarkdown { get; set; }
    }
}
