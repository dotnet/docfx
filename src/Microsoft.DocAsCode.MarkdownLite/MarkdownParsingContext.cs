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

        public MarkdownParsingContext(SourceInfo lineInfo)
        {
            CurrentMarkdown = lineInfo.Markdown;
            _markdownLength = lineInfo.Markdown.Length;
            _file = lineInfo.File;
            _lineNumber = lineInfo.LineNumber;
            _lineIndexer = CreateLineIndexer(lineInfo.Markdown);
        }

        public string CurrentMarkdown { get; private set; }

        public SourceInfo LineInfo => default(SourceInfo);

        public SourceInfo Consume(int charCount)
        {
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
            return new SourceInfo(markdown, _file, _lineNumber + CalcLineNumber());
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
