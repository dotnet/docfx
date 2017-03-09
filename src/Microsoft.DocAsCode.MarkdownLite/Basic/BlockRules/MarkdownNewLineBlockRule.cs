// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownNewLineBlockRule : IMarkdownRule
    {
        private static readonly Matcher _NewLineMatcher = Matcher.Repeat(Matcher.Char('\n'), 1);

        public virtual string Name => "NewLine";

        [Obsolete("Please use NewLineMatcher.", true)]
        public virtual Regex NewLine => Regexes.Block.Newline;

        public virtual Matcher NewLineMatcher => _NewLineMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = context.Match(NewLineMatcher);
            if (match?.Length > 0)
            {
                return new MarkdownNewLineBlockToken(this, parser.Context, context.Consume(match.Length));
            }
            return null;
        }
    }
}
