// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    public struct SourceInfo
    {
        private SourceInfo(string markdown, string file, int lineNumber, int validLineCount)
        {
            Markdown = markdown;
            File = file;
            LineNumber = lineNumber;
            ValidLineCount = validLineCount;
        }

        public string Markdown { get; }

        public string File { get; }

        public int LineNumber { get; }

        public int ValidLineCount { get; }

        public static SourceInfo Create(string markdown, string file, int lineNumber = 1)
        {
            return new SourceInfo(markdown, file, lineNumber, GetValidLineCount(markdown));
        }

        public SourceInfo Copy(string markdown, int lineOffset = 0)
        {
            return new SourceInfo(markdown, File, LineNumber + lineOffset, GetValidLineCount(markdown));
        }

        private static int GetValidLineCount(string markdown)
        {
            var validLineCount = 0;
            var trimEndNewLine = markdown.Trim('\n');
            for (var i = trimEndNewLine.Length - 1; i >= 0; i--)
            {
                if (trimEndNewLine[i] == '\n')
                    validLineCount++;
            }
            return validLineCount;
        }
    }
}
