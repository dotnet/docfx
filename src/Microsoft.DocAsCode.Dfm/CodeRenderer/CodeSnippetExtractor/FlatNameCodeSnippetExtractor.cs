// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Flat name extrator works for snippet name exists in both start and end line representations
    /// E.g., comment representation needs snippet name exists in both start and end line comment
    /// </summary>
    public class FlatNameCodeSnippetExtractor : CodeSnippetRegexExtractor
    {
        private readonly Regex _startLineRegex;
        private readonly Regex _endLineRegex;

        public FlatNameCodeSnippetExtractor(Regex startLineRegex, Regex endLineRegex)
        {
            _startLineRegex = startLineRegex ?? throw new ArgumentNullException(nameof(startLineRegex));
            _endLineRegex = endLineRegex ?? throw new ArgumentNullException(nameof(endLineRegex));
        }

        protected override List<CodeSnippetTag> ResolveCodeSnippetTags(string[] lines)
        {
            var snippetTags = new List<CodeSnippetTag>();
            for (int i = 0; i < lines.Length; i++)
            {
                var startLineMatch = _startLineRegex.Match(lines[i]);
                if (startLineMatch.Success)
                {
                    string tagName = startLineMatch.Groups["name"].Value;
                    snippetTags.Add(new CodeSnippetTag(tagName, i + 1, CodeSnippetTagType.Start));
                    continue;
                }

                var endLineMatch = _endLineRegex.Match(lines[i]);
                if (endLineMatch.Success)
                {
                    string tagName = endLineMatch.Groups["name"].Value;
                    snippetTags.Add(new CodeSnippetTag(tagName, i + 1, CodeSnippetTagType.End));
                }
            }

            return snippetTags;
        } 
    }
}