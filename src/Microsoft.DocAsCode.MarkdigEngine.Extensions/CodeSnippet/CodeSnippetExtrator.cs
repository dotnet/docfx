// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using Markdig.Helpers;

    public class CodeSnippetExtrator
    {
        private static readonly Regex TagnameFormat = new Regex(@"^[\w\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly string StartLineTemplate;
        private readonly string EndLineTemplate;
        private readonly bool IsEndLineContainsTagName;
        public const string TagNamePlaceHolder = "{tagname}";

        private readonly MarkdownContext _context;

        public CodeSnippetExtrator(string startLineTemplate, string endLineTemplate, MarkdownContext context, bool isEndLineContainsTagName = true)
        {
            this.StartLineTemplate = startLineTemplate;
            this.EndLineTemplate = endLineTemplate;
            this.IsEndLineContainsTagName = isEndLineContainsTagName;

            _context = context;
        }

        public Dictionary<string, CodeRange> GetAllTags(string[] lines, ref HashSet<int> tagLines)
        {
            var result = new Dictionary<string, CodeRange>(StringComparer.OrdinalIgnoreCase);
            var tagStack = new Stack<string>();

            for(int index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                string tagName;

                if(MatchTag(line, EndLineTemplate, out tagName, IsEndLineContainsTagName))
                {
                    tagLines.Add(index);
                    if (!IsEndLineContainsTagName)
                    {
                        tagName = tagStack.Count > 0 ? tagStack.Pop() : string.Empty;
                    }

                    if(result.ContainsKey(tagName))
                    {
                        if (result[tagName].End == 0)
                        {
                            // we meet the first end tag, ignore the following ones
                            result[tagName].End = index;
                        }
                    }

                    continue;
                }

                if (MatchTag(line, StartLineTemplate, out tagName))
                {
                    tagLines.Add(index);
                    result[tagName] = new CodeRange { Start = index + 2 };
                    tagStack.Push(tagName);
                }

            }

            return result;
        }

        private bool MatchTag(string line, string template, out string tagName, bool containTagname = true)
        {
            tagName = string.Empty;
            if (string.IsNullOrEmpty(line) || string.IsNullOrEmpty(template)) return false;

            var splitedTemplate = template.Split(new[] { TagNamePlaceHolder }, StringSplitOptions.None);
            var beforeTagName = splitedTemplate[0];
            var afterTagName = splitedTemplate.Length == 2 ? splitedTemplate[1] : string.Empty;

            int column = 0;
            int index = 0;

            // match before
            while (column < line.Length && index < beforeTagName.Length)
            {
                if (!CharHelper.IsWhitespace(line[column]))
                {
                    if (char.ToLower(line[column]) != beforeTagName[index]) return false;
                    index++;
                }
                column++;
            }

            if (index != beforeTagName.Length) return false;

            //match tagname
            var sb = new StringBuilder();
            while(column < line.Length && (afterTagName == string.Empty || line[column] != afterTagName[0]))
            {
                sb.Append(line[column]);
                column++;
            }
            tagName = sb.ToString().Trim().ToLower();

            //match after tagname
            index = 0;
            while(column < line.Length && index < afterTagName.Length)
            {
                if (!CharHelper.IsWhitespace(line[column]))
                {
                    if (char.ToLower(line[column]) != afterTagName[index]) return false;
                    index++;
                }
                column++;
            }

            if (index != afterTagName.Length) return false;
            while (column < line.Length && CharHelper.IsWhitespace(line[column])) column++;

            return column == line.Length && (!containTagname || TagnameFormat.IsMatch(tagName));
        }
    }
}
