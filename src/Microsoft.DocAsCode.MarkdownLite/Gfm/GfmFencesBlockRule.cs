﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class GfmFencesBlockRule : IMarkdownRule
    {
        private static readonly Matcher _EndFences =
            Matcher.NewLine.RepeatAtLeast(1) +
            Matcher.WhiteSpace.Repeat(0, 3) +
            Matcher.BackReference("flag").RepeatAtLeast(3).CompareLength(LengthComparison.GreaterThanOrEquals, "flagLength") +
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine | Matcher.EndOfString);
        private static readonly Matcher _FencesMatcher =
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.Char('`').ToGroup("flag").RepeatAtLeast(3) | Matcher.Char('~').ToGroup("flag").RepeatAtLeast(3)).ToGroup("flagLength") +
            Matcher.WhiteSpacesOrEmpty +
            Matcher.AnyCharNotIn(' ', '\n').RepeatAtLeast(0).ToGroup("lang") +
            Matcher.WhiteSpacesOrEmpty + Matcher.NewLine +
            (
                _EndFences.ToNegativeTest() +
                (Matcher.AnyStringInSingleLine | Matcher.NewLine.RepeatAtLeast(1))
            ).RepeatAtLeast(0).ToGroup("code") +
            (_EndFences | Matcher.EndOfString);

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
