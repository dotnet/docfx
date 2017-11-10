// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownHeadingBlockRule : IMarkdownRule
    {
        private static readonly Matcher _EndSymbol =
            (Matcher.WhiteSpaces + Matcher.Char('#').RepeatAtLeast(1)).Maybe() +
            Matcher.WhiteSpacesOrEmpty +
            (Matcher.NewLine | Matcher.EndOfString);
        private static readonly Matcher _HeadingMatcher =
            Matcher.WhiteSpacesOrEmpty +
            Matcher.Char('#').Repeat(1, 6).ToGroup("level") +
            Matcher.WhiteSpaces +
            (
                _EndSymbol.ToNegativeTest() +
                (Matcher.AnyCharNotIn('\n', ' ').RepeatAtLeast(1) | Matcher.WhiteSpaces)
            ).RepeatAtLeast(1).ToGroup("text") +
            _EndSymbol;

        public virtual string Name => "Heading";

        [Obsolete("Please use HeadingMatcher.")]
        public virtual Regex Heading => Regexes.Block.Heading;

        public virtual Matcher HeadingMatcher => _HeadingMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Heading != Regexes.Block.Heading || parser.Options.LegacyMode)
            {
                return OldMatch(parser, context);
            }
            var match = context.Match(HeadingMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new TwoPhaseBlockToken(
                    this,
                    parser.Context,
                    sourceInfo,
                    (p, t) => new MarkdownHeadingBlockToken(
                        t.Rule,
                        t.Context,
                        p.TokenizeInline(t.SourceInfo.Copy(match["text"].GetValue())),
                        Regex.Replace(match["text"].GetValue().ToLower(), @"[^\p{L}\p{N}\- ]+", "").Replace(' ', '-'),
                        match["level"].Count,
                        t.SourceInfo));
            }
            return null;
        }

        private IMarkdownToken OldMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Heading.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                sourceInfo,
                (p, t) => new MarkdownHeadingBlockToken(
                    t.Rule,
                    t.Context,
                    p.TokenizeInline(t.SourceInfo.Copy(match.Groups[2].Value)),
                    Regex.Replace(match.Groups[2].Value.ToLower(), @"[^\p{L}\p{N}\- ]+", "").Replace(' ', '-'),
                    match.Groups[1].Value.Length,
                    t.SourceInfo));
        }
    }
}
