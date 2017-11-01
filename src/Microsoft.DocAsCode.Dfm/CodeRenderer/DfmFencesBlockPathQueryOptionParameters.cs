// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Web;

    using Microsoft.DocAsCode.Common;

    public class DfmFencesBlockPathQueryOptionParameters
    {
        private static readonly Regex _dfmFencesSharpQueryStringRegex = new Regex(@"^L(?<start>\d+)\-L(?<end>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
        private static readonly Regex _dfmFencesRangeQueryStringRegex = new Regex(@"^(?<start>\d+)\-(?<end>\d+)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
        private const string StartLineQueryStringKey = "start";
        private const string EndLineQueryStringKey = "end";
        private const string TagNameQueryStringKey = "name";
        private const string RangeQueryStringKey = "range";
        private const string HighlightLinesQueryStringKey = "highlight";
        private const string DedentQueryStringKey = "dedent";
        private const char RegionSeparatorInRangeQueryString = ',';

        public List<Tuple<int?, int?>> LinePairs { get; set; } = new List<Tuple<int?, int?>>();

        public string HighlightLines { get; set; }

        public int? DedentLength { get; set; }

        public string TagName { get; set; }

        public static DfmFencesBlockPathQueryOptionParameters Create(string queryAndFragment)
        {
            var result = new DfmFencesBlockPathQueryOptionParameters();

            if (string.IsNullOrEmpty(queryAndFragment))
            {
                return result;
            }

            var queryOption = queryAndFragment.Remove(1);
            var queryString = queryAndFragment.Substring(1);
            int startLine, endLine;

            if (queryOption == "#")
            {
                // check if line number representation
                var match = _dfmFencesSharpQueryStringRegex.Match(queryString);
                if (match.Success && int.TryParse(match.Groups["start"].Value, out startLine) && int.TryParse(match.Groups["end"].Value, out endLine))
                {
                    result.LinePairs.Add(new Tuple<int?, int?>(startLine, endLine));
                }
                else
                {
                    result.TagName = queryString;
                }
            }
            else if (queryOption == "?")
            {
                var collection = HttpUtility.ParseQueryString(queryString);
                result.TagName = collection[TagNameQueryStringKey];
                result.HighlightLines = collection[HighlightLinesQueryStringKey];
                var start = int.TryParse(collection[StartLineQueryStringKey], out startLine) ? startLine : (int?)null;
                var end = int.TryParse(collection[EndLineQueryStringKey], out endLine) ? endLine: (int?)null;
                var range = collection[RangeQueryStringKey];
                if (collection[DedentQueryStringKey] != null)
                {
                    if (int.TryParse(collection[DedentQueryStringKey], out int dedentTemp))
                    {
                        result.DedentLength = dedentTemp;
                    }
                    else
                    {
                        Logger.LogWarning($"Illegal dedent `{collection[DedentQueryStringKey]}` in query parameter `dedent`. Auto-dedent will be applied.");
                    }
                }
                if (range != null)
                {
                    var regions = range.Split(RegionSeparatorInRangeQueryString);
                    if (regions != null)
                    {
                        foreach (var region in regions)
                        {
                            var match = _dfmFencesRangeQueryStringRegex.Match(region);
                            if (match.Success)
                            {
                                // consider region as `{startlinenumber}-{endlinenumber}`, in which {endlinenumber} is optional
                                result.LinePairs.Add(new Tuple<int?, int?>(
                                    int.TryParse(match.Groups["start"].Value, out startLine) ? startLine : (int?)null,
                                    int.TryParse(match.Groups["end"].Value, out endLine) ? endLine : (int?)null
                                ));
                            }
                            else
                            {
                                // consider region as a sigine line number
                                var tempLine = int.TryParse(region, out int line) ? line : (int?)null;
                                result.LinePairs.Add(new Tuple<int?, int?>(tempLine, tempLine));
                            }
                        }
                    }
                }
                else if (start != null || end != null)
                {
                    result.LinePairs.Add(new Tuple<int?, int?>(start, end));
                }
            }

            return result;
        }
    }
}
