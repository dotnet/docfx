// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.Common;

    public abstract class DfmFencesRule : IMarkdownRule
    {
        private const string StartLineQueryStringKey = "start";
        private const string EndLineQueryStringKey = "end";
        private const string TagNameQueryStringKey = "name";
        private const string RangeQueryStringKey = "range";
        private const string HighlightLinesQueryStringKey = "highlight";
        private const string DedentQueryStringKey = "dedent";
        private const char RegionSeparatorInRangeQueryString = ',';

        public abstract string Name { get; }

        private static readonly Regex _dfmFencesSharpQueryStringRegex = new Regex(@"^L(?<start>\d+)\-L(?<end>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
        internal static readonly Regex _dfmFencesRangeQueryStringRegex = new Regex(@"^(?<start>\d+)\-(?<end>\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        public abstract IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context);

        [Obsolete("use DfmCodeExtractor.ParsePathQueryString")]
        public static IDfmFencesBlockPathQueryOption ParsePathQueryString(string queryOption, string queryString)
        {
            return ParsePathQueryString(queryOption, queryString, false);
        }

        [Obsolete("use DfmCodeExtractor.ParsePathQueryString")]
        public static IDfmFencesBlockPathQueryOption ParsePathQueryString(string queryOption, string queryString, bool noCache = false)
        {
            if (string.IsNullOrEmpty(queryOption) || string.IsNullOrEmpty(queryString))
            {
                return null;
            }

            int startLine, endLine;
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
                    return new TagNameBlockPathQueryOption(noCache) { TagName = queryString};
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
                    if (int.TryParse(collection[DedentQueryStringKey], out int dedentTemp))
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
                    return new TagNameBlockPathQueryOption(noCache) { TagName = tagName , HighlightLines = highlight, DedentLength = dedent};
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
                                var tempLine = int.TryParse(region, out int line) ? line : (int?)null;
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
