// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownLHeadingBlockRule : IMarkdownRule
    {
        // ^([^\n]+)\n *(=|-){2,} *(?:\n+|$)
        private static readonly Matcher _LHeadingMatcher =
            // @"([^\n]+)\n"
            Matcher.AnyStringInSingleLine.ToGroup("text") + Matcher.NewLine +
            // @" *(=|-){2,} *"
            Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('=', '-').RepeatAtLeast(2).ToGroup("level") + Matcher.WhiteSpacesOrEmpty +
            // @"(?:\n+|$)"
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        public virtual string Name => "LHeading";

        [Obsolete("Please use LHeadingMatcher.")]
        public virtual Regex LHeading => Regexes.Block.LHeading;

        public virtual Matcher LHeadingMatcher => _LHeadingMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (LHeading != Regexes.Block.LHeading || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(LHeadingMatcher);
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
                        context.Markdown[match["level"].StartIndex] == '=' ? 1 : 2,
                        t.SourceInfo));
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = LHeading.Match(context.CurrentMarkdown);
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
                    p.TokenizeInline(t.SourceInfo.Copy(match.Groups[1].Value)),
                    Regex.Replace(match.Groups[1].Value.ToLower(), @"[^\p{L}\p{N}\- ]+", "").Replace(' ', '-'),
                    match.Groups[2].Value == "=" ? 1 : 2,
                    t.SourceInfo));
        }
    }
}
