// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public struct SourceInfo
    {
        public SourceInfo(string markdown, string file, int lineNumber)
        {
            Markdown = markdown;
            File = file;
            LineNumber = lineNumber;
        }

        public string Markdown { get; }

        public string File { get; }

        public int LineNumber { get; }

        public SourceInfo Copy(string markdown, int lineOffset = 0)
        {
            return new SourceInfo(markdown, File, LineNumber + lineOffset);
        }
    }
}
