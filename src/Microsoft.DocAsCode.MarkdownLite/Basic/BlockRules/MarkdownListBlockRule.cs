// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite.Matchers;

    public class MarkdownListBlockRule : IMarkdownRule
    {
        private static readonly Matcher _ChildList =
            Matcher.WhiteSpacesOrEmpty.CompareLength(LengthComparison.GreaterThanOrEquals, "indent") +
            ((Matcher.AnyCharInRange('0', '9').RepeatAtLeast(1) + '.') | Matcher.AnyCharIn('*', '+', '-')) +
            Matcher.WhiteSpaces;
        private static readonly Matcher _FollowingText =
            Matcher.AnyStringInSingleLine + (Matcher.NewLine | Matcher.EndOfString) +
            (
                // todo : not other block rule
                (
                    Matcher.WhiteSpacesOrEmpty.CompareLength(LengthComparison.LessThan, "indent") +
                    (
                        // blockquote
                        Matcher.Char('>') |
                        // list
                        (((Matcher.AnyCharInRange('0', '9').RepeatAtLeast(1) + '.') | Matcher.AnyCharIn('*', '+', '-')) + Matcher.WhiteSpaces) |
                        // heading
                        (Matcher.Char('#').Repeat(1, 6) + Matcher.WhiteSpaces + Matcher.AnyCharNotIn(' ', '\n')) |
                        // hr
                        ((Matcher.AnyCharIn('-', '*', '_') + Matcher.WhiteSpacesOrEmpty).RepeatAtLeast(3) + Matcher.NewLine) |
                        // fence
                        ((Matcher.Char('`').RepeatAtLeast(3) | Matcher.Char('~').RepeatAtLeast(3)) + Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharNotIn(' ', '\n').RepeatAtLeast(0) + Matcher.WhiteSpacesOrEmpty + Matcher.NewLine)
                    )
                ).ToNegativeTest() +
                Matcher.AnyStringInSingleLine + (Matcher.NewLine | Matcher.EndOfString)
            ).RepeatAtLeast(0);

        private static readonly Matcher _OrderListMatcher =
            // @" *(\d+. *)"
            (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharInRange('0', '9').RepeatAtLeast(1).ToGroup("start") + '.' + Matcher.WhiteSpaces).ToGroup("indent") +
            (
                (
                    // following text.
                    _FollowingText +
                    // following empty lines
                    (
                        // child list
                        (Matcher.NewLine.RepeatAtLeast(0) + _ChildList + _FollowingText) |
                        // next list item, update indent
                        (Matcher.NewLine.RepeatAtLeast(0) + (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharInRange('0', '9').RepeatAtLeast(1) + '.' + Matcher.WhiteSpaces).ToGroup("indent") + _FollowingText) |
                        // other
                        (Matcher.NewLine.RepeatAtLeast(1) + Matcher.WhiteSpaces.CompareLength(LengthComparison.GreaterThanOrEquals, "indent") + _FollowingText)
                    ).RepeatAtLeast(0)
                ) |
                Matcher.EndOfString
            );
        private static readonly Matcher _UnorderListMatcher =
            // @" *(\d+. *)"
            (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('*', '+', '-') + Matcher.WhiteSpaces).ToGroup("indent") +
            (
                (
                    // following text.
                    _FollowingText +
                    // following empty lines
                    (
                        // child list
                        (Matcher.NewLine.RepeatAtLeast(0) + _ChildList + _FollowingText) |
                        // next list item, update indent
                        (Matcher.NewLine.RepeatAtLeast(0) + (Matcher.WhiteSpacesOrEmpty + Matcher.AnyCharIn('*', '+', '-') + Matcher.WhiteSpaces).ToGroup("indent") + _FollowingText) |
                        // other
                        (Matcher.NewLine.RepeatAtLeast(1) + Matcher.WhiteSpaces.CompareLength(LengthComparison.GreaterThanOrEquals, "indent") + _FollowingText)
                    ).RepeatAtLeast(0)
                ) |
                Matcher.EndOfString
            );

        public virtual string Name => "List";

        [Obsolete("Please use OrderListMatcher.")]
        public virtual Regex OrderList => Regexes.Block.OrderList;

        [Obsolete("Please use ListMatcher.")]
        public virtual Regex UnorderList => Regexes.Block.UnorderList;

        public virtual Matcher OrderListMatcher => _OrderListMatcher;

        public virtual Matcher UnorderListMatcher => _UnorderListMatcher;

        public virtual Regex Item => Regexes.Block.Item;

        public virtual Regex Bullet => Regexes.Block.Bullet;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            if (OrderList != Regexes.Block.OrderList ||
                UnorderList != Regexes.Block.UnorderList ||
                parser.Options.LegacyMode)
            {
                return TryMatchOld(parser, context);
            }
            var match = context.Match(OrderListMatcher);
            int start = 1;
            bool ordered;
            if (match == null)
            {
                match = context.Match(UnorderListMatcher);
                ordered = false;
            }
            else
            {
                start = int.Parse(match["start"].GetValue());
                ordered = true;
            }
            if (match?.Length > 0)
            {
                var sourceInfo = context.Consume(match.Length);
                var cap = sourceInfo.Markdown.Match(Item);
                var next = false;
                var l = cap.Length;
                int i = 0;
                var tokens = new List<IMarkdownToken>();
                var lineOffset = 0;
                var lines = 0;
                for (; i < l; i++)
                {
                    var item = cap[i];
                    lines = CountLine(item);
                    // Remove the list item's bullet
                    // so it is seen as the next token.
                    var space = item.Length;
                    item = item.ReplaceRegex(Regexes.Lexers.LeadingBullet, string.Empty);

                    // Outdent whatever the
                    // list item contains. Hacky.
                    if (item.IndexOf("\n ") > -1)
                    {
                        space -= item.Length;
                        item = !parser.Options.Pedantic
                          ? Regex.Replace(item, "^ {1," + space + "}", "", RegexOptions.Multiline)
                          : Regex.Replace(item, @"^ {1,4}", "", RegexOptions.Multiline);
                    }

                    // Determine whether item is loose or not.
                    // Use: /(^|\n)(?! )[^\n]+\n\n(?!\s*$)/
                    // for discount behavior.
                    var loose = next || Regex.IsMatch(item, @"\n\n(?!\s*$)");
                    if (i != l - 1 && item.Length != 0)
                    {
                        next = item[item.Length - 1] == '\n';
                        if (!loose) loose = next;
                    }

                    var c = parser.SwitchContext(MarkdownBlockContext.IsTop, false);
                    var itemSourceInfo = sourceInfo.Copy(item, lineOffset);
                    var blockTokens = parser.Tokenize(itemSourceInfo);
                    parser.SwitchContext(c);
                    blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, loose, itemSourceInfo);
                    tokens.Add(new MarkdownListItemBlockToken(this, parser.Context, blockTokens, loose, itemSourceInfo));
                    lineOffset += lines;
                }

                return new MarkdownListBlockToken(this, parser.Context, tokens.ToImmutableArray(), ordered, start, sourceInfo);
            }
            return null;
        }

        private IMarkdownToken TryMatchOld(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = OrderList.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                match = UnorderList.Match(context.CurrentMarkdown);
                if (match.Length == 0)
                {
                    return null;
                }
            }
            var sourceInfo = context.Consume(match.Length);

            var bull = match.Groups[2].Value;

            var cap = match.Groups[0].Value.Match(Item);

            var next = false;
            var l = cap.Length;
            int i = 0;
            var tokens = new List<IMarkdownToken>();
            var lineOffset = 0;
            var lines = 0;
            for (; i < l; i++)
            {
                var item = cap[i];
                lines = CountLine(item);
                // Remove the list item's bullet
                // so it is seen as the next token.
                var space = item.Length;
                item = item.ReplaceRegex(Regexes.Lexers.LeadingBullet, string.Empty);

                // Outdent whatever the
                // list item contains. Hacky.
                if (item.IndexOf("\n ") > -1)
                {
                    space -= item.Length;
                    item = !parser.Options.Pedantic
                      ? Regex.Replace(item, "^ {1," + space + "}", "", RegexOptions.Multiline)
                      : Regex.Replace(item, @"^ {1,4}", "", RegexOptions.Multiline);
                }

                // Determine whether item is loose or not.
                // Use: /(^|\n)(?! )[^\n]+\n\n(?!\s*$)/
                // for discount behavior.
                var loose = next || Regex.IsMatch(item, @"\n\n(?!\s*$)");
                if (i != l - 1 && item.Length != 0)
                {
                    next = item[item.Length - 1] == '\n';
                    if (!loose) loose = next;
                }

                var c = parser.SwitchContext(MarkdownBlockContext.IsTop, false);
                if (!loose)
                {
                    var bc = (MarkdownBlockContext)parser.Context;
                    parser.SwitchContext(
                        bc.SetRules(
                            ImmutableList.Create<IMarkdownRule>(
                                this,
                                new MarkdownNewLineBlockRule(),
                                new MarkdownTextBlockRule())));
                }
                var itemSourceInfo = sourceInfo.Copy(item, lineOffset);
                var blockTokens = parser.Tokenize(itemSourceInfo);
                parser.SwitchContext(c);
                blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, loose, itemSourceInfo);
                tokens.Add(new MarkdownListItemBlockToken(this, parser.Context, blockTokens, loose, itemSourceInfo));
                lineOffset += lines;
            }

            return new MarkdownListBlockToken(this, parser.Context, tokens.ToImmutableArray(), bull.Length > 1, sourceInfo);
        }

        private static int CountLine(string item)
        {
            var count = 1;
            for (int i = 0; i < item.Length; i++)
            {
                if (item[i] == '\n')
                {
                    count++;
                }
            }
            return count;
        }
    }
}
