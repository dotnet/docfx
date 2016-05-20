// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    public class MarkdownParsingContext : IMarkdownParsingContext
    {
        private readonly LineInfo _lineInfo;
        private readonly int _markdownLength;
        private readonly List<int> _lineIndexer;
        private int _offset;

        public MarkdownParsingContext(string markdown, LineInfo lineInfo)
        {
            CurrentMarkdown = markdown;
            _lineInfo = lineInfo;
            _markdownLength = markdown.Length;
            _lineIndexer = CreateLineIndexer(markdown);
        }

        public string CurrentMarkdown { get; private set; }

        public LineInfo LineInfo => _lineInfo.Move(_offset);

        public void Consume(int charCount)
        {
            CurrentMarkdown = CurrentMarkdown.Substring(charCount);
            _offset = CalcLineNumber();
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
