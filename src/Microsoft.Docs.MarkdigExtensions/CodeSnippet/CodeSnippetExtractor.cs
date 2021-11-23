// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Markdig.Helpers;

namespace Microsoft.Docs.MarkdigExtensions;

public class CodeSnippetExtractor
{
    public const string TagNamePlaceHolder = "{tagname}";

    private static readonly Regex s_tagnameFormat = new(@"^[\w\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string _startLineTemplate;
    private readonly string _endLineTemplate;
    private readonly bool _isEndLineContainsTagName;

    public CodeSnippetExtractor(string startLineTemplate, string endLineTemplate, bool isEndLineContainsTagName = true)
    {
        _startLineTemplate = startLineTemplate;
        _endLineTemplate = endLineTemplate;
        _isEndLineContainsTagName = isEndLineContainsTagName;
    }

    public Dictionary<string, CodeRange> GetAllTags(string[] lines, ref HashSet<int> tagLines)
    {
        var result = new Dictionary<string, CodeRange>(StringComparer.OrdinalIgnoreCase);
        var tagStack = new Stack<string>();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];

            if (MatchTag(line, _endLineTemplate, out var tagName, _isEndLineContainsTagName))
            {
                tagLines.Add(index);
                if (!_isEndLineContainsTagName)
                {
                    tagName = tagStack.Count > 0 ? tagStack.Pop() : "";
                }

                if (result.ContainsKey(tagName))
                {
                    if (result[tagName].End == 0)
                    {
                        // we meet the first end tag, ignore the following ones
                        result[tagName].End = index;
                    }
                }

                continue;
            }

            if (MatchTag(line, _startLineTemplate, out tagName))
            {
                tagLines.Add(index);
                result[tagName] = new CodeRange { Start = index + 2 };
                tagStack.Push(tagName);
            }
        }

        return result;
    }

    private static bool MatchTag(string line, string template, out string tagName, bool containTagname = true)
    {
        tagName = "";
        if (string.IsNullOrEmpty(line) || string.IsNullOrEmpty(template))
        {
            return false;
        }

        var splitedTemplate = template.Split(new[] { TagNamePlaceHolder }, StringSplitOptions.None);
        var beforeTagName = splitedTemplate[0];
        var afterTagName = splitedTemplate.Length == 2 ? splitedTemplate[1] : "";

        var column = 0;
        var index = 0;

        // match before
        while (column < line.Length && index < beforeTagName.Length)
        {
            if (!CharHelper.IsWhitespace(line[column]))
            {
                if (char.ToLowerInvariant(line[column]) != beforeTagName[index])
                {
                    return false;
                }

                index++;
            }
            column++;
        }

        if (index != beforeTagName.Length)
        {
            return false;
        }

        // match tagname
        var sb = new StringBuilder();
        while (column < line.Length && (string.IsNullOrEmpty(afterTagName) || line[column] != afterTagName[0]))
        {
            sb.Append(line[column]);
            column++;
        }
        tagName = sb.ToString().Trim().ToLowerInvariant();

        // match after tagname
        index = 0;
        while (column < line.Length && index < afterTagName.Length)
        {
            if (!CharHelper.IsWhitespace(line[column]))
            {
                if (char.ToLowerInvariant(line[column]) != afterTagName[index])
                {
                    return false;
                }

                index++;
            }
            column++;
        }

        if (index != afterTagName.Length)
        {
            return false;
        }

        while (column < line.Length && CharHelper.IsWhitespace(line[column]))
        {
            column++;
        }

        return column == line.Length && (!containTagname || s_tagnameFormat.IsMatch(tagName));
    }
}
