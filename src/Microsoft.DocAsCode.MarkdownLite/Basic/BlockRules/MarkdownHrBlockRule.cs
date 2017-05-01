// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownHrBlockRule : IMarkdownRule
    {
        // ^( *[-*_]){3,} *(?:\n+|$)
        private static readonly Matcher _HrMatcher =
            // @"( *[-*_]){3,} *"
            (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('-', '*', '_')).RepeatAtLeast(3) + Matcher.WhiteSpacesOrEmpty +
            // @"(?:\n+|$)"
            (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        public virtual string Name => "Hr";

        [Obsolete("Please use HrMatcher.")]
        public virtual Regex Hr => Regexes.Block.Hr;

        public virtual Matcher HrMatcher => _HrMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Hr != Regexes.Block.Hr || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(HrMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownHrBlockToken(this, parser.Context, sourceInfo);
            }
            return null;
        }

        public virtual IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Hr.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownHrBlockToken(this, parser.Context, sourceInfo);
        }
    }
}
