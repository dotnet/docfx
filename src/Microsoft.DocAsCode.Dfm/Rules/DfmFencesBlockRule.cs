// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class DfmFencesBlockRule : DfmFencesRule
    {
        private static readonly Matcher _DfmFencesMatcher =
            Matcher.WhiteSpacesOrEmpty + "[!" + Matcher.CaseInsensitiveString("code") +
            (Matcher.Char('-') + (Matcher.AnyWordCharacter | Matcher.Char('-')).RepeatAtLeast(1).ToGroup("lang")).Maybe() +
            Matcher.WhiteSpacesOrEmpty + '[' +
            (
                Matcher.AnyCharNotIn('\\', ']').RepeatAtLeast(1) |
                (Matcher.Char('\\') + Matcher.AnyCharNot('\n'))
            ).RepeatAtLeast(0).ToGroup("name") +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            '(' +
            Matcher.WhiteSpacesOrEmpty +
            (
                (Matcher.AnyCharNotIn(')', '\n', '\\', ' ').RepeatAtLeast(1) | (Matcher.Char('\\') + Matcher.AnyCharNot('\n')) | Matcher.WhiteSpaces + Matcher.AnyCharIn('\'', '"').ToNegativeTest()).RepeatAtLeast(1).ToGroup("href") |
                (Matcher.Char('<') + (Matcher.AnyCharNotIn(')', '>', '\n', '\\').RepeatAtLeast(1) | (Matcher.Char('\\') + Matcher.AnyCharNot('\n'))).RepeatAtLeast(1).ToGroup("href") + '>')
            ) +
            Matcher.WhiteSpacesOrEmpty +
            (
                (Matcher.Char('\'') + (Matcher.AnyCharNot('\'').RepeatAtLeast(1) | (Matcher.Char('\\') + Matcher.AnyCharNot('\n'))).RepeatAtLeast(0).ToGroup("title") + '\'') |
                (Matcher.Char('"') + (Matcher.AnyCharNot('"').RepeatAtLeast(1) | (Matcher.Char('\\') + Matcher.AnyCharNot('\n'))).RepeatAtLeast(0).ToGroup("title") + '"')
            ).Maybe() +
            Matcher.WhiteSpacesOrEmpty +
            ')' +
            Matcher.WhiteSpacesOrEmpty +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        private static readonly Regex _dfmFencesRegex = new Regex(@"^ *\[\!(?:(?i)code(?:\-(?<lang>[\w|\-]+))?)\s*\[(?<name>(?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?(?<path>(?:[^\n\]]|\\\])*?)((?<option>[\#|\?])(?<optionValue>\S+))?>?(?:\s+(?<quote>['""])(?<title>[\s\S]*?)\k<quote>)?\s*\)\]\s*(?:\n|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

        public override string Name => "DfmFences";

        public virtual Matcher DfmFencesMatcher => _DfmFencesMatcher;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(DfmFencesMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);

                // [!code-lang[name](href "optionalTitle")]
                var name = StringHelper.UnescapeMarkdown(match["name"].GetValue());
                var href = StringHelper.UnescapeMarkdown(match["href"].GetValue());
                var lang = match.GetGroup("lang")?.GetValue() ?? string.Empty;
                var title = StringHelper.UnescapeMarkdown(match.GetGroup("title")?.GetValue() ?? string.Empty);
                var queryStringAndFragment = UriUtility.GetQueryStringAndFragment(href);
                var path = UriUtility.GetPath(href);
                return new DfmFencesBlockToken(this, parser.Context, name, path, sourceInfo, lang, title, queryStringAndFragment);
            }
            return null;
        }

        [Obsolete]
        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
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

            return new DfmFencesBlockToken(this, parser.Context, name, path, sourceInfo, lang, title, pathQueryOption, pathQueryOption != null ? match.Groups["option"]?.Value + match.Groups["optionValue"]?.Value : null);
        }

    }
}
