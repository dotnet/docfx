// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownBlockquoteBlockRule : IMarkdownRule
    {
        private static readonly Matcher _BlockquoteMatcher =
            (
                // @" *> *\n"
                (Matcher.WhiteSpacesOrEmpty + '>' + Matcher.WhiteSpacesOrEmpty + (Matcher.NewLine | Matcher.EndOfString)) |
                (
                    // @" *>[^\n]+(\n[^\n]+)*"
                    Matcher.WhiteSpacesOrEmpty + '>' + Matcher.AnyStringInSingleLine +
                    (
                        Matcher.NewLine +
                        // expect following:
                        (
                            // heading
                            (Matcher.WhiteSpacesOrEmpty + Matcher.Char('#').Repeat(1, 6) + Matcher.WhiteSpaces + Matcher.AnyStringInSingleLine + '\n') |
                            // hr
                            ((Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('-', '*', '_')).RepeatAtLeast(3) + Matcher.WhiteSpacesOrEmpty + '\n') |
                            // list
                            (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('-', '*') + Matcher.WhiteSpaces + Matcher.AnyStringInSingleLine + '\n') |
                            (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharInRange('0', '9').RepeatAtLeast(1) + '.' + Matcher.WhiteSpaces + Matcher.AnyStringInSingleLine + '\n') |
                            // @" *>"
                            (Matcher.WhiteSpacesOrEmpty + '>')
                        ).ToNegativeTest() +
                        Matcher.AnyStringInSingleLine
                    ).RepeatAtLeast(0) +
                    (Matcher.NewLine | Matcher.EndOfString)
                )
            ).RepeatAtLeast(1) +
            Matcher.NewLine.RepeatAtLeast(0);

        public virtual string Name => "Blockquote";

        [Obsolete("Please use BlockquoteMatcher.")]
        public virtual Regex Blockquote => Regexes.Block.Blockquote;

        public virtual Matcher BlockquoteMatcher => _BlockquoteMatcher;

        public virtual Regex LeadingBlockquote => Regexes.Lexers.LeadingBlockquote;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (Blockquote != Regexes.Block.Blockquote || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(BlockquoteMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                var capStr = LeadingBlockquote.Replace(sourceInfo.Markdown, string.Empty);
                var blockTokens = parser.Tokenize(sourceInfo.Copy(capStr));
                blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, true, sourceInfo);
                return new MarkdownBlockquoteBlockToken(
                    this,
                    parser.Context,
                    blockTokens,
                    sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Blockquote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var capStr = LeadingBlockquote.Replace(sourceInfo.Markdown, string.Empty);
            var blockTokens = parser.Tokenize(sourceInfo.Copy(capStr));
            blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, true, sourceInfo);
            return new MarkdownBlockquoteBlockToken(
                this,
                parser.Context,
                blockTokens,
                sourceInfo);
        }
    }
}
