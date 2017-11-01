// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class CodeSnippetRegexExtractor : ICodeSnippetExtractor
    {
        public Dictionary<string, DfmTagNameResolveResult> GetAll(string[] lines)
        {
            if (lines == null)
            {
                throw new ArgumentNullException(nameof(lines));
            }

            var snippetTags = ResolveCodeSnippetTags(lines);
            var snippetTagGroups = snippetTags.GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase);

            var excludedLines = new HashSet<int>(from tagGroup in snippetTagGroups
                                                 from tag in tagGroup
                                                 select tag.Line);

            var result = new Dictionary<string, DfmTagNameResolveResult>();
            foreach (var snippetTagGroup in snippetTagGroups.Where(g => !string.IsNullOrEmpty(g.Key)))
            {
                var tagResolveResult = new DfmTagNameResolveResult {IsSuccessful = true};
                string tagName = snippetTagGroup.Key;

                var startTags = (from tag in snippetTagGroup
                                 where tag.Type == CodeSnippetTagType.Start
                                 select tag.Line).ToList();

                var endTags = (from tag in snippetTagGroup
                               where tag.Type == CodeSnippetTagType.End
                               select tag.Line).ToList();

                if (startTags.Count != 1 || endTags.Count != 1)
                {
                    tagResolveResult.ErrorMessage =
                        $"Tag {tagName} is not paired or occurred just more than once, details: ({startTags.Count} start lines, {endTags.Count} end lines)";
                }

                if (startTags.Count == 0 || endTags.Count == 0)
                {
                    tagResolveResult.IsSuccessful = false;
                }
                else
                {
                    int startLine = startTags[startTags.Count - 1];
                    int endLine = endTags[0];
                    if (startLine < endLine)
                    {
                        tagResolveResult.StartLine = startLine + 1;
                        tagResolveResult.EndLine = endLine - 1;
                        tagResolveResult.ExcludesLines = excludedLines;
                    }
                    else
                    {
                        tagResolveResult.IsSuccessful = false;
                        tagResolveResult.ErrorMessage = $"Tag {tagName}'s start line '{startLine}' should be less than end line '{endLine}'";
                    }
                }

                result.Add(tagName, tagResolveResult);
            }

            return result;
        }

        protected abstract List<CodeSnippetTag> ResolveCodeSnippetTags(string[] lines);
    }
}