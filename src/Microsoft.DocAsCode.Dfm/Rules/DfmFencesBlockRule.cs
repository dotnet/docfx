// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using System.Web;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmFencesBlockRule : IMarkdownRule
    {
        private const string StartLineQueryStringKey = "start";
        private const string EndLineQueryStringKey = "end";
        private const string TagNameQueryStringKey = "name";
        private const string RangeQueryStringKey = "range";
        private const char RegionSeparatorInRangeQueryString = ',';

        public string Name => "RestApiFences";

        public static readonly Regex _dfmFencesRegex = new Regex(@"^\[\!((?i)code(\-(?<lang>[\w|\-]+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>[\s\S]*?)((?<option>[\#|\?])(?<optionValue>\S+))?>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled);
        public static readonly Regex _dfmFencesSharpQueryStringRegex = new Regex(@"^L(?<start>\d+)\-L(?<end>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex _dfmFencesRangeQueryStringRegex = new Regex(@"^(?<start>\d+)\-(?<end>\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = _dfmFencesRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!code-REST-i[name](path "optionalTitle")]
            var name = match.Groups["name"].Value;
            var path = match.Groups["path"].Value;
            var lang = match.Groups["lang"]?.Value;
            var title = match.Groups["title"]?.Value;
            var pathQueryOption = ParsePathQueryString(match.Groups["option"]?.Value, match.Groups["optionValue"]?.Value);

            return new DfmFencesBlockToken(this, engine.Context, name, path, match.Value, lang, title, pathQueryOption);
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
                if (tagName != null)
                {
                    return new TagNameBlockPathQueryOption { TagName = tagName };
                }
                else if (range != null)
                {
                    var regions = range.Split(RegionSeparatorInRangeQueryString);
                    if (regions != null)
                    {
                        var option = new MultipleLineRangeBlockPathQueryOption();
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
                        EndLine = int.TryParse(end, out endLine) ? endLine : (int?)null
                    };
                }
                return null;
            }
            return null;
        }
    }
}
