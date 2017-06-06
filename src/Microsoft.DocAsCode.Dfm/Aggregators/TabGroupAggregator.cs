// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class TabGroupAggregator : MarkdownTokenAggregator<MarkdownHeadingBlockToken>
    {
        private static readonly Regex HrefRegex = new Regex(@"^#tab\/([a-zA-Z0-9\-]+)(?:\/([a-zA-Z0-9\-]+)?)?$", RegexOptions.Compiled);

        protected override bool AggregateCore(MarkdownHeadingBlockToken headToken, IMarkdownTokenAggregateContext context)
        {
            var pair = ParseHeading(headToken);
            if (pair == null)
            {
                return false;
            }
            int offset = 1;
            var items = new List<DfmTabItemBlockToken>();
            var list = new List<IMarkdownToken>();
            while (true)
            {
                var token = context.LookAhead(offset);
                switch (token)
                {
                    case MarkdownHeadingBlockToken head:
                        var temp = ParseHeading(head);
                        if (temp == null)
                        {
                            goto default;
                        }
                        items.Add(CreateTabItem(headToken, pair, list));
                        pair = temp;
                        list.Clear();
                        break;
                    case MarkdownHrBlockToken hr:
                        offset++;
                        goto case null;
                    case null:
                        items.Add(CreateTabItem(headToken, pair, list));
                        // todo : rule, source info
                        context.AggregateTo(
                            new DfmTabGroupBlockToken(
                                headToken.Rule,
                                headToken.Context,
                                Guid.NewGuid().ToString(),
                                items.ToImmutableArray(),
                                0,
                                headToken.SourceInfo),
                            offset);
                        return true;
                    default:
                        list.Add(token);
                        break;
                }
                offset++;
            }
        }

        private static DfmTabItemBlockToken CreateTabItem(
            MarkdownHeadingBlockToken headToken,
            Tuple<string, string, InlineContent> pair,
            List<IMarkdownToken> list)
        {
            // todo : rule, source info
            var title = new DfmTabTitleBlockToken(
                headToken.Rule,
                headToken.Context,
                pair.Item3,
                headToken.SourceInfo);
            var content = new DfmTabContentBlockToken(
                headToken.Rule,
                headToken.Context,
                list.ToImmutableArray(),
                headToken.SourceInfo);
            return new DfmTabItemBlockToken(
                headToken.Rule,
                headToken.Context,
                pair.Item1,
                pair.Item2,
                title,
                content,
                headToken.SourceInfo);
        }

        private static Tuple<string, string, InlineContent> ParseHeading(MarkdownHeadingBlockToken headToken)
        {
            if (headToken.Content.Tokens.Length == 1 &&
                headToken.Content.Tokens[0] is MarkdownLinkInlineToken link)
            {
                var m = HrefRegex.Match(link.Href);
                if (m.Success)
                {
                    return Tuple.Create(m.Groups[1].Value, m.Groups[2].Value, new InlineContent(link.Content));
                }
            }
            return null;
        }
    }
}
