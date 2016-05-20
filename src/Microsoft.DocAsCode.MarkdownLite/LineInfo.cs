// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public struct LineInfo
    {
        public LineInfo(string file, int lineNumber)
        {
            File = file;
            LineNumber = lineNumber;
        }

        public string File { get; }

        public int LineNumber { get; }

        public bool HasInfo => File != null && LineNumber != 0;

        public LineInfo Move(int offset)
        {
            if (!HasInfo)
            {
                return this;
            }
            return new LineInfo(File, LineNumber + offset);
        }
    }
}
