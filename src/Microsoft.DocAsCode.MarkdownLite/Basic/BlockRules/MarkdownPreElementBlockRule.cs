// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownPreElementBlockRule : IMarkdownRule
    {
        private static readonly Matcher _ender =
            Matcher.CaseInsensitiveString("/pre") +
            Matcher.AnyCharIn(' ', '\n').RepeatAtLeast(0) +
            '>';
        private static readonly Matcher _preElementMatcher =
            Matcher.WhiteSpacesOrEmpty +
            Matcher.CaseInsensitiveString("<pre") +
            Matcher.AnyCharIn(' ', '\n', '>').ToTest() +
            (
                Matcher.AnyCharNotIn('<').RepeatAtLeast(0) |
                (Matcher.Char('<') + _ender.ToNegativeTest())
            ).RepeatAtLeast(0) +
            '<' +
            _ender +
            Matcher.AnyStringInSingleLineOrEmpty +
            Matcher.NewLine.RepeatAtLeast(1);

        public virtual string Name => "Block.Html.PreElement";

        public virtual Regex PreElement => Regexes.Block.PreElement;

        public virtual Matcher PreElementMatcher => _preElementMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (PreElement != Regexes.Block.PreElement || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(PreElementMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownRawToken(this, parser.Context, sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = PreElement.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownRawToken(this, parser.Context, sourceInfo);
        }
    }
}
