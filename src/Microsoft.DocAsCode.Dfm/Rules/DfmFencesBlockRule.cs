// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
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
                Matcher.AnyCharNot(']').RepeatAtLeast(1) |
                (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.Char(']'))
            ).RepeatAtLeast(0).ToGroup("name") +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            '(' +
            (
                (Matcher.AnyCharNotIn(')', '\n', ' ').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.Char(')'))).RepeatAtLeast(1).ToGroup("href") |
                (Matcher.Char('<') + (Matcher.AnyCharNotIn(')', '>', '\n', ' ').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.AnyCharIn(')', '>'))).RepeatAtLeast(1).ToGroup("href") + '>')
            ) +
            Matcher.WhiteSpacesOrEmpty +
            (
                (Matcher.Char('\'') + (Matcher.AnyCharNot('\'').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + '\'')).RepeatAtLeast(0).ToGroup("title") + '\'') |
                (Matcher.Char('"') + (Matcher.AnyCharNot('"').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + '"')).RepeatAtLeast(0).ToGroup("title") + '"')
            ).Maybe() +
            Matcher.WhiteSpacesOrEmpty +
            ')' +
            Matcher.WhiteSpacesOrEmpty +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        public override string Name => "DfmFences";

        public virtual Matcher DfmFencesMatcher => _DfmFencesMatcher;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
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
                var pathQueryOption =
                    !string.IsNullOrEmpty(queryStringAndFragment) ?
                    ParsePathQueryString(queryStringAndFragment.Remove(1), queryStringAndFragment.Substring(1)) :
                    null;
                return new DfmFencesBlockToken(this, parser.Context, name, path, sourceInfo, lang, title, pathQueryOption);
            }
            return null;
        }
    }
}
