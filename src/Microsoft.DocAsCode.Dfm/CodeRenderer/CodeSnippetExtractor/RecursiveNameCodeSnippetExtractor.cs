// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Recursive name extrator works for snippet name only exists in start line representation
    /// E.g., C# region representation only has snippet name in start line representation
    /// </summary>
    public class RecursiveNameCodeSnippetExtractor : CodeSnippetRegexExtractor
    {
        private readonly Regex _startLineRegex;
        private readonly Regex _endLineRegex;

        public RecursiveNameCodeSnippetExtractor(Regex startLineRegex, Regex endLineRegex)
        {
            _startLineRegex = startLineRegex ?? throw new ArgumentNullException(nameof(startLineRegex));
            _endLineRegex = endLineRegex ?? throw new ArgumentNullException(nameof(endLineRegex));
        }

        protected override List<CodeSnippetTag> ResolveCodeSnippetTags(string[] lines)
        {
            var snippetList = new List<CodeSnippetTag>();

            // TODO: consider region has begin representation without end representation, current they are ignored
            var snippetStack = new Stack<CodeSnippetTag>();
            for (int i = 0; i < lines.Length; i++)
            {
                var startLineMatch = _startLineRegex.Match(lines[i]);
                if (startLineMatch.Success)
                {
                    string tagName = startLineMatch.Groups["name"].Value;
                    snippetStack.Push(new CodeSnippetTag(tagName, i + 1, CodeSnippetTagType.Start));
                    continue;
                }

                var endLineMatch = _endLineRegex.Match(lines[i]);
                if (endLineMatch.Success)
                {
                    if (snippetStack.Count == 0)
                    {
                        throw new Exception($"line {i} contains an end line that can't match a start line");
                    }
                    var codeSnippetTag = snippetStack.Pop();
                    snippetList.Add(codeSnippetTag);
                    snippetList.Add(new CodeSnippetTag(codeSnippetTag.Name, i + 1, CodeSnippetTagType.End));
                }
            }

            return snippetList;
        }
    }
}