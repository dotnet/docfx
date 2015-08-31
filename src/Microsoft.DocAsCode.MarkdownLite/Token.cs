// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public class Token
    {

        public string Text { get; set; }

        public TokenType Type { get; set; }

        public int Depth { get; set; }

        public bool Escaped { get; set; }

        public string Lang { get; set; }

        public bool Ordered { get; set; }

        public bool Pre { get; set; }

        public string[] Header { get; set; }

        public Align[] Align { get; set; }

        public string[][] Cells { get; set; }

    }
}
