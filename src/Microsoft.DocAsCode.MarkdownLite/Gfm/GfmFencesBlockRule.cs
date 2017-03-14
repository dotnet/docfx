// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class GfmFencesBlockRule : IMarkdownRule
    {
        private static readonly Matcher _FencesMatcher =
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.Char('`').RepeatAtLeast(3) | Matcher.Char('~').RepeatAtLeast(3)).ToGroup("flag") +
            Matcher.WhiteSpacesOrEmpty +
            Matcher.AnyCharNotIn(' ', '\n').RepeatAtLeast(0).ToGroup("lang") +
            Matcher.WhiteSpacesOrEmpty + Matcher.NewLine +
            (
                (Matcher.NewLine.RepeatAtLeast(1) + Matcher.WhiteSpacesOrEmpty + Matcher.BackReference("flag") + Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine | Matcher.EndOfString)).ToNegativeTest() +
                (Matcher.AnyCharNot('\n').RepeatAtLeast(1) | Matcher.NewLine.RepeatAtLeast(1))
            ).RepeatAtLeast(0).ToGroup("code") +
            Matcher.NewLine.RepeatAtLeast(1) + Matcher.WhiteSpacesOrEmpty + Matcher.BackReference("flag") + Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        public virtual string Name => "Fences";

        public virtual Matcher FencesMatcher => null;

        [Obsolete("Please use FencesMatcher.", true)]
        public virtual Regex Fences => Regexes.Block.Gfm.Fences;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = context.Match(_FencesMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownCodeBlockToken(this, parser.Context, match["code"].GetValue(), match["lang"].GetValue(), sourceInfo);
            }
            return null;
        }
    }
}
