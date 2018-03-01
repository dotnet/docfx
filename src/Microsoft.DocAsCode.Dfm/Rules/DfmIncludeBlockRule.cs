// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class DfmIncludeBlockRule : IMarkdownRule
    {
        private static readonly Matcher _IncludeMatcher =
            Matcher.WhiteSpacesOrEmpty +
            "[!" +
            Matcher.CaseInsensitiveString("include") +
            Matcher.Char('+').Maybe() +
            Matcher.WhiteSpacesOrEmpty +
            '[' +
            (
                Matcher.AnyCharNot(']').RepeatAtLeast(1) |
                (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.Char(']'))
            ).RepeatAtLeast(0).ToGroup("name") +
            ']' +
            Matcher.WhiteSpacesOrEmpty +
            '(' +
            (
                (Matcher.AnyCharNotIn(')', '\n', ' ').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.Char(')'))).RepeatAtLeast(1).ToGroup("path") |
                (Matcher.Char('<') + (Matcher.AnyCharNotIn(')', '>', '\n', ' ').RepeatAtLeast(1) | (Matcher.ReverseTest(Matcher.Char('\\')) + Matcher.AnyCharIn(')', '>'))).RepeatAtLeast(1).ToGroup("path") + '>')
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

        private static readonly Regex _incRegex = new Regex(@"^\[!INCLUDE\+?\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));
        public virtual string Name => "DfmIncludeBlock";
        [Obsolete]
        public virtual Regex Include => _incRegex;
        public virtual Matcher IncludeMatcher => _IncludeMatcher;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Include != _incRegex || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(IncludeMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);

                // [!include[name](path "title")]
                var path = match["path"].GetValue();
                var name = match["name"].GetValue();
                var title = match.GetGroup("title")?.GetValue() ?? string.Empty;

                return new DfmIncludeBlockToken(
                    this,
                    parser.Context,
                    StringHelper.UnescapeMarkdown(path),
                    StringHelper.UnescapeMarkdown(name),
                    StringHelper.UnescapeMarkdown(title),
                    sourceInfo);
            }
            return null;
        }

        [Obsolete]
        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Include.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            // [!include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            return new DfmIncludeBlockToken(this, parser.Context, path, value, title, sourceInfo);
        }
    }
}
