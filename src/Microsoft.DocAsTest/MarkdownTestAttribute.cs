// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;

namespace Microsoft.DocAsTest
{
    /// <summary>
    /// Provides test data coming from markdown fences.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MarkdownTestAttribute : Attribute, ITestAttribute
    {
        /// <summary>
        /// Gets or sets the directory to search for files.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the search pattern.
        /// </summary>
        public string SearchPattern { get; set; }

        /// <summary>
        /// Gets or sets the markdown fenced code tip to match. The default is to match all.
        /// </summary>
        public string FenceTip { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of opening fence chars (`) to match. The default is 3.
        /// </summary>
        public int MinFenceChar { get; set; } = 3;

        public MarkdownTestAttribute(string path = null)
        {
            Path = path;
            SearchPattern = "*.md";
        }

        private enum MarkdownReadState
        {
            Markdown,
            Fence,
        }

        void ITestAttribute.DiscoverTests(string path, Action<TestData> report)
        {
            using (var reader = File.OpenText(path))
            {
                var state = MarkdownReadState.Fence;
                var indent = 0;
                var ordinal = 0;
                var lineNumber = 0;
                var data = new TestData();
                var content = new StringBuilder();

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line is null)
                        return;

                    lineNumber++;

                    switch (state)
                    {
                        case MarkdownReadState.Markdown when ReadStartFence(line, out indent, out var currentTip):
                            data.FenceTip = currentTip;
                            data.LineNumber = lineNumber;
                            state = MarkdownReadState.Fence;
                            content.Length = 0;
                            break;

                        case MarkdownReadState.Markdown when !string.IsNullOrWhiteSpace(line):
                            data.Summary = line;
                            break;

                        case MarkdownReadState.Fence when ReadEndFence(line, indent):
                            data.Ordinal = ordinal++;
                            data.Content = content.ToString();
                            data.FilePath = path;
                            report(data);
                            data = new TestData();
                            state = MarkdownReadState.Markdown;
                            break;

                        case MarkdownReadState.Fence:
                            if (line.Length > indent)
                                content.Append(line, indent, line.Length - indent);
                            content.AppendLine();
                            break;
                    }
                }
            }
        }

        private bool ReadStartFence(string line, out int indent, out string tip)
        {
            tip = null;

            var i = 0;

            while (i < line.Length && line[i] == ' ')
                i++;

            indent = i;

            var backtick = i;

            while (i < line.Length && line[i] == '`')
                i++;

            if (i - backtick < MinFenceChar)
                return false;

            tip = line.Substring(i).Trim();

            return string.IsNullOrEmpty(FenceTip) || FenceTip.Equals(tip, StringComparison.OrdinalIgnoreCase);
        }

        private bool ReadEndFence(string line, int indent)
        {
            var i = 0;

            while (i < line.Length && line[i] == ' ')
                i++;

            if (i < indent)
                return false;

            var backtick = i;

            while (i < line.Length && line[i] == '`')
                i++;

            if (i - backtick < 3)
                return false;

            while (i < line.Length && line[i] == ' ')
                i++;

            return i == line.Length;
        }
    }
}
