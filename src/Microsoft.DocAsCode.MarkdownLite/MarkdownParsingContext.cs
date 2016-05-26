// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    public class MarkdownParsingContext : IMarkdownParsingContext
    {
        private readonly int _markdownLength;
        private readonly List<int> _lineIndexer;
        private readonly string _file;
        private readonly int _lineNumber;

        public MarkdownParsingContext(SourceInfo sourceInfo)
        {
            CurrentMarkdown = sourceInfo.Markdown;
            _markdownLength = sourceInfo.Markdown.Length;
            _file = sourceInfo.File;
            _lineNumber = sourceInfo.LineNumber;
            _lineIndexer = CreateLineIndexer(sourceInfo.Markdown);
        }

        public string CurrentMarkdown { get; private set; }

        public int LineNumber => _lineNumber;

        public string File => _file;

        public SourceInfo Consume(int charCount)
        {
            var offset = CalcLineNumber();
            string markdown;
            if (CurrentMarkdown.Length == charCount)
            {
                markdown = CurrentMarkdown;
                CurrentMarkdown = string.Empty;
            }
            else
            {
                markdown = CurrentMarkdown.Remove(charCount);
                CurrentMarkdown = CurrentMarkdown.Substring(charCount);
            }
            return SourceInfo.Create(markdown, _file, _lineNumber + offset);
        }

        private static List<int> CreateLineIndexer(string markdown)
        {
            var lineIndexer = new List<int>();
            var index = markdown.IndexOf('\n');
            while (index != -1)
            {
                lineIndexer.Add(index);
                if (index == markdown.Length - 1)
                {
                    break;
                }
                index = markdown.IndexOf('\n', index + 1);
            }
            return lineIndexer;
        }

        private int CalcLineNumber()
        {
            var charIndex = _markdownLength - CurrentMarkdown.Length;
            var index = _lineIndexer.BinarySearch(charIndex);
            if (index >= 0)
            {
                return index;
            }
            else
            {
                return ~index;
            }
        }
    }
}
