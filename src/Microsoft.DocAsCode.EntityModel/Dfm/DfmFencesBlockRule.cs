// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;
    using System.Web;

    using MarkdownLite;

    public class DfmFencesBlockRule : IMarkdownRule
    {
        private const string StartLineQueryStringKey = "start";
        private const string EndLineQueryStringKey = "end";
        private const string TagNameQueryStringKey = "name";

        public string Name => "RestApiFences";

        public static readonly Regex _dfmFencesRegex = new Regex(@"^\[\!((?i)code(\-(?<lang>[\w|\-]+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>[\s\S]*?)((?<option>[\#|\?])(?<optionValue>\S+))?>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled);
        public static readonly Regex _dfmFencesSharpQueryStringRegex = new Regex(@"^L(?<start>\d+)\-L(?<end>\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual IMarkdownToken TryMatch(MarkdownParser engine, ref string source)
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

            return new DfmFencesBlockToken(this, engine.Context, name, path, lang, title, pathQueryOption);
        }

        private static DfmFencesBlockPathQueryOption ParsePathQueryString(string queryOption, string queryString)
        {
            if (string.IsNullOrEmpty(queryOption) || string.IsNullOrEmpty(queryString))
            {
                return null;
            }

            long startLine, endLine;
            if (queryOption == "#")
            {
                // check if line number representation
                var match = _dfmFencesSharpQueryStringRegex.Match(queryString);
                if (match.Success && long.TryParse(match.Groups["start"].Value, out startLine) && long.TryParse(match.Groups["end"].Value, out endLine))
                {
                    return new DfmFencesBlockPathQueryOption { StartLine = startLine, EndLine = endLine };
                }
                else
                {
                    return new DfmFencesBlockPathQueryOption { TagName = queryString };
                }
            }
            else if (queryOption == "?")
            {
                var collection = HttpUtility.ParseQueryString(queryString);
                var tagName = collection[TagNameQueryStringKey];
                var start = collection[StartLineQueryStringKey];
                var end = collection[EndLineQueryStringKey];
                if (tagName != null)
                {
                    return new DfmFencesBlockPathQueryOption { TagName = tagName };
                }
                else if (start != null || end != null)
                {
                    return new DfmFencesBlockPathQueryOption
                    {
                        StartLine = long.TryParse(start, out startLine) ? startLine : (long?)null,
                        EndLine = long.TryParse(end, out endLine) ? endLine : (long?)null
                    };
                }
            }

            return new DfmFencesBlockPathQueryOption();
        }
    }
}
