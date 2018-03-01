// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;
    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class DfmVideoBlockRule : IMarkdownRule
    {
        private static readonly Matcher _VideoMatcher =
            Matcher.WhiteSpacesOrEmpty + Matcher.CaseInsensitiveString("[!video") + Matcher.WhiteSpaces +
            (
                Matcher.String("http") + Matcher.Char('s').Maybe() + "://" +
                (
                    Matcher.AnyCharNotIn('\n', ' ', ']').RepeatAtLeast(1) |
                    (Matcher.ReverseTest(Matcher.Char('\\')) + ']')
                ).RepeatAtLeast(1)
            ).ToGroup("link") +
            Matcher.WhiteSpacesOrEmpty + ']' + Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        private static readonly Regex _videoRegex = new Regex(@"^ *\[\!Video +(?<link>https?\:\/\/.+?) *\] *(\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10));

        public virtual string Name => "DfmVideoBlock";

        [Obsolete("Please use VideoMatcher.")]
        public virtual Regex VideoRegex => _videoRegex;

        public virtual Matcher VideoMatcher => _VideoMatcher;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (!parser.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)parser.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            if (VideoRegex != _videoRegex || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(VideoMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new DfmVideoBlockToken(this, parser.Context, StringHelper.UnescapeMarkdown(match["link"].GetValue()), sourceInfo);
            }
            return null;
        }

        [Obsolete]
        public IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = VideoRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            // [!Video https://]
            var link = StringHelper.LegacyUnescapeMarkdown(match.Groups["link"].Value);
            return new DfmVideoBlockToken(this, parser.Context, link, sourceInfo);
        }
    }
}
