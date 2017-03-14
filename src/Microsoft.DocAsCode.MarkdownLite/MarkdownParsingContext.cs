// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownParsingContext : IMarkdownParsingContext
    {
        private readonly string _markdown;
        private int _startIndex;
        private readonly List<int> _lineIndexer;
        private readonly string _file;
        private readonly int _lineNumber;
        private string _currentMarkdown;

        public MarkdownParsingContext(SourceInfo sourceInfo)
        {
            _markdown = sourceInfo.Markdown;
            _currentMarkdown = _markdown;
            _file = sourceInfo.File;
            _lineNumber = sourceInfo.LineNumber;
            _lineIndexer = CreateLineIndexer(sourceInfo.Markdown);
        }

        public string Markdown => _markdown;

        public string CurrentMarkdown
        {
            get
            {
                if (_currentMarkdown == null)
                {
                    _currentMarkdown =
                        _startIndex == _markdown.Length ?
                        string.Empty :
                        _markdown.Substring(_startIndex);
                }
                return _currentMarkdown;
            }
        }

        public bool IsInParagraph { get; set; }

        public int LineNumber => _lineNumber;

        public string File => _file;

        public SourceInfo ToSourceInfo()
        {
            return SourceInfo.Create(CurrentMarkdown, _file, _lineNumber);
        }

        public SourceInfo Consume(int charCount)
        {
            var offset = CalcLineNumber();
            string markdown = _markdown.Substring(_startIndex, charCount);
            _startIndex += charCount;
            _currentMarkdown = null;
            return SourceInfo.Create(markdown, _file, _lineNumber + offset);
        }

        public MatchResult Match(Matcher matcher)
        {
            return matcher.Match(_markdown, _startIndex);
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
            var index = _lineIndexer.BinarySearch(_startIndex);
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
