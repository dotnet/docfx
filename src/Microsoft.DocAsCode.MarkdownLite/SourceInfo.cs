// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Runtime.CompilerServices;

    public struct SourceInfo
    {
        private int _validLineCount;

        private SourceInfo(string markdown, string file, int lineNumber, int validLineCount)
        {
            Markdown = markdown;
            File = file;
            LineNumber = lineNumber;
            _validLineCount = validLineCount;
        }

        public string Markdown { get; }

        public string File { get; }

        public int LineNumber { get; }

        public int ValidLineCount
        {
            get
            {
                if (_validLineCount <= 0)
                {
                    _validLineCount = GetValidLineCount(Markdown);
                }
                return _validLineCount;
            }
        }

        public static SourceInfo Create(string markdown, string file)
        {
            return Create(markdown, file, 1);
        }

        public static SourceInfo Create(string markdown, string file, int lineNumber)
        {
            return new SourceInfo(markdown, file, lineNumber, 0);
        }

        public static SourceInfo Create(string markdown, string file, int lineNumber, int lineCount)
        {
            return new SourceInfo(markdown, file, lineNumber, lineCount == 0 ? 0 : GetValidLineCount(markdown, lineCount));
        }

        public SourceInfo Copy(string markdown, int lineOffset = 0)
        {
            return new SourceInfo(markdown, File, LineNumber + lineOffset, 0);
        }

        private static int GetValidLineCount(string markdown)
        {
            if (markdown == "")
                return 0;
            var indexOfLastChar = markdown.Length - 1;
            var validLineCount = 1;

            while (indexOfLastChar >= 0 && markdown[indexOfLastChar] == '\n')
                indexOfLastChar--;

            for (var i = indexOfLastChar - 1; i >= 0; i--)
            {
                if (markdown[i] == '\n')
                    validLineCount++;
            }

            return validLineCount;
        }

        private static int GetValidLineCount(string markdown, int lineCount)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return 0;
            }
            var indexOfLastChar = markdown.Length - 1;
            var validLineCount = lineCount;

            while (indexOfLastChar >= 0 && markdown[indexOfLastChar--] == '\n')
            {
                validLineCount--;
            }
            return validLineCount;
        }
    }
}
