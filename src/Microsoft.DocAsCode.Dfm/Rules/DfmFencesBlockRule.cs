﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Common;

    public class DfmFencesBlockRule : IMarkdownRule
    {
        private const string StartLineQueryStringKey = "start";
        private const string EndLineQueryStringKey = "end";
        private const string TagNameQueryStringKey = "name";
        private const string RangeQueryStringKey = "range";
        private const string HighlightLinesQueryStringKey = "highlight";
        private const string DedentQueryStringKey = "dedent";
        private const char RegionSeparatorInRangeQueryString = ',';

        public string Name => "DfmFences";

        public static readonly Regex _dfmFencesRegex = new Regex(@"^ *\[\!((?i)code(\-(?<lang>[\w|\-]+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>[^\n]*?)((?<option>[\#|\?])(?<optionValue>\S+))?>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled);
        public static readonly Regex _dfmFencesSharpQueryStringRegex = new Regex(@"^L(?<start>\d+)\-L(?<end>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex _dfmFencesRangeQueryStringRegex = new Regex(@"^(?<start>\d+)\-(?<end>\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = _dfmFencesRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            // [!code-REST-i[name](path "optionalTitle")]
            var name = match.Groups["name"].Value;
            var path = match.Groups["path"].Value;
            var lang = match.Groups["lang"]?.Value;
            var title = match.Groups["title"]?.Value;
            var pathQueryOption = ParsePathQueryString(match.Groups["option"]?.Value, match.Groups["optionValue"]?.Value);

            return new DfmFencesBlockToken(this, parser.Context, name, path, sourceInfo, lang, title, pathQueryOption);
        }

        private static IDfmFencesBlockPathQueryOption ParsePathQueryString(string queryOption, string queryString)
        {
            if (string.IsNullOrEmpty(queryOption) || string.IsNullOrEmpty(queryString))
            {
                return null;
            }

            int startLine, endLine, line;
            if (queryOption == "#")
            {
                // check if line number representation
                var match = _dfmFencesSharpQueryStringRegex.Match(queryString);
                if (match.Success && int.TryParse(match.Groups["start"].Value, out startLine) && int.TryParse(match.Groups["end"].Value, out endLine))
                {
                    return new LineRangeBlockPathQueryOption { StartLine = startLine, EndLine = endLine };
                }
                else
                {
                    return new TagNameBlockPathQueryOption { TagName = queryString };
                }
            }
            else if (queryOption == "?")
            {
                var collection = HttpUtility.ParseQueryString(queryString);
                var tagName = collection[TagNameQueryStringKey];
                var start = collection[StartLineQueryStringKey];
                var end = collection[EndLineQueryStringKey];
                var range = collection[RangeQueryStringKey];
                var highlight = collection[HighlightLinesQueryStringKey];
                int? dedent = null;
                if (collection[DedentQueryStringKey] != null)
                {
                    int dedentTemp;
                    if (int.TryParse(collection[DedentQueryStringKey], out dedentTemp))
                    {
                        dedent = dedentTemp;
                    }
                    else
                    {
                        Logger.LogWarning($"Illegal dedent `{collection[DedentQueryStringKey]}` in query parameter `dedent`. Auto-dedent will be applied.");
                    }
                }
                if (tagName != null)
                {
                    return new TagNameBlockPathQueryOption { TagName = tagName , HighlightLines = highlight, DedentLength = dedent};
                }
                else if (range != null)
                {
                    var regions = range.Split(RegionSeparatorInRangeQueryString);
                    if (regions != null)
                    {
                        var option = new MultipleLineRangeBlockPathQueryOption { HighlightLines = highlight, DedentLength = dedent};
                        foreach (var region in regions)
                        {
                            var match = _dfmFencesRangeQueryStringRegex.Match(region);
                            if (match.Success)
                            {
                                // consider region as `{startlinenumber}-{endlinenumber}`, in which {endlinenumber} is optional
                                option.LinePairs.Add(new Tuple<int?, int?>(
                                    int.TryParse(match.Groups["start"].Value, out startLine) ? startLine : (int?)null,
                                    int.TryParse(match.Groups["end"].Value, out endLine) ? endLine : (int?)null
                                ));
                            }
                            else
                            {
                                // consider region as a sigine line number
                                var tempLine = int.TryParse(region, out line) ? line : (int?)null;
                                option.LinePairs.Add(new Tuple<int?, int?>(tempLine, tempLine));
                            }
                        }
                        return option;
                    }
                }
                else if (start != null || end != null)
                {
                    return new LineRangeBlockPathQueryOption
                    {
                        StartLine = int.TryParse(start, out startLine) ? startLine : (int?)null,
                        EndLine = int.TryParse(end, out endLine) ? endLine : (int?)null,
                        HighlightLines = highlight,
                        DedentLength = dedent,
                    };
                }
                return new FullFileBlockPathQueryOption { HighlightLines = highlight, DedentLength = dedent };
            }
            return new FullFileBlockPathQueryOption();
        }
    }
}
