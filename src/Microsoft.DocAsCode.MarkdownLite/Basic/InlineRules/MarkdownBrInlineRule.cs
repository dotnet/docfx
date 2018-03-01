// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownBrInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Br";

        [Obsolete]
        public virtual Regex Br => Regexes.Inline.Br;

        public virtual Matcher BrMatcher =>
            (((Matcher)' ').RepeatAtLeast(2) | (Matcher)'\\') +
            (Matcher)'\n' +
            (Matcher.BlankOrEmpty + Matcher.EndOfString).ToNegativeTest();

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(BrMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownBrInlineToken(this, parser.Context, sourceInfo);
            }
            return null;
        }

        [Obsolete]
        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Br.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownBrInlineToken(this, parser.Context, sourceInfo);
        }
    }
}
