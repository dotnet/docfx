// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmFencesInlineRule : DfmFencesRule
    {
        public override string Name => "DfmFencesInline";

        private static readonly Regex _dfmFencesRegex = new Regex(@"^ *\[\!((?i)code(\-(?<lang>[\w|\-]+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>[^\n]*?)((?<option>[\#|\?])(?<optionValue>\S+))?>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.GetIsInTable())
            {
                if (!parser.Options.LegacyMode)
                {
                    return null;
                }
            }
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
            var queryStringAndFragment = match.Groups["option"]?.Value + match.Groups["optionValue"]?.Value;

            if (!parser.Context.GetIsInTable())
            {
                Logger.LogWarning("Inline code snippet is only allowed inside tables.", line: sourceInfo.LineNumber.ToString(), code: WarningCodes.Markdown.InvalidInlineCodeSnippet);
            }
            return new DfmFencesBlockToken(this, parser.Context, name, path, sourceInfo, lang, title, queryStringAndFragment);
        }
    }
}
