// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class GfmHtmlCommentBlockRule : IMarkdownRule
    {
        private static readonly Matcher _HtmlCommentMatcher =
            Matcher.WhiteSpacesOrEmpty +
            // @"<!--(?:[^-]|-(?!->))*-->"
            Matcher.String("<!--") +
            (
                Matcher.AnyCharNot('-').RepeatAtLeast(1) |
                (Matcher.Char('-') + Matcher.String("->").ToNegativeTest())
            ).RepeatAtLeast(0) + "-->" +
            Matcher.AnyStringInSingleLineOrEmpty + (Matcher.NewLine.RepeatAtLeast(1) | Matcher.EndOfString);

        public virtual string Name => "GfmHtmlComment";

        [Obsolete("Please use HtmlCommentMatcher.")]
        public virtual Regex HtmlComment => Regexes.Block.Gfm.HtmlComment;

        public virtual Matcher HtmlCommentMatcher => _HtmlCommentMatcher;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (HtmlComment != Regexes.Block.Gfm.HtmlComment || parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(HtmlCommentMatcher);
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                return new MarkdownHtmlBlockToken(
                    this,
                    parser.Context,
                    new InlineContent(
                        ImmutableArray.Create<IMarkdownToken>(
                            new MarkdownRawToken(
                                this,
                                parser.Context,
                                sourceInfo))),
                    sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = HtmlComment.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownHtmlBlockToken(
                this,
                parser.Context,
                new InlineContent(
                    ImmutableArray.Create<IMarkdownToken>(
                        new MarkdownRawToken(
                            this,
                            parser.Context,
                            sourceInfo))),
                sourceInfo);
        }
    }
}
