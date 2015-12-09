// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using MarkdownLite;

    public class DfmFencesBlockRule : IMarkdownRule
    {
        public string Name => "RestApiFences";

        public static readonly Regex _dfmFencesRegex = new Regex(@"^\[!((?i)code(-(?<lang>(\w|-)+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>[\s\S]*?)>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled);

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = _dfmFencesRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!code-REST-i[title](path "optionalTitle")]
            var name = match.Groups["name"].Value;
            var path = match.Groups["path"].Value;
            var lang = match.Groups["lang"]?.Value;
            var title = match.Groups["title"]?.Value;

            return new DfmFencesBlockToken(this, name, path, lang, title);
        }
    }
}
