// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class TabGroupAggregator : MarkdownTokenAggregator<MarkdownHeadingBlockToken>
    {
        private static readonly Regex HrefRegex = new Regex(@"^#tab\/(?<id>[a-zA-Z0-9\-]+(?:\+[a-zA-Z0-9\-]+)*)(?:\/(?<condition>[a-zA-Z0-9\-]+)?)?$", RegexOptions.Compiled);

        protected override bool AggregateCore(MarkdownHeadingBlockToken headToken, IMarkdownTokenAggregateContext context)
        {
            var info = ParseHeading(headToken);
            if (info == null)
            {
                return false;
            }
            int offset = 1;
            var items = new List<DfmTabItemBlockToken>();
            IMarkdownToken terminator = null;
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
                        items.Add(CreateTabItem(info));
                        info = temp;
                        break;
                    case MarkdownHrBlockToken hr:
                        terminator = hr;
                        goto case null;
                    case null:
                        items.Add(CreateTabItem(info));
                        AggregateCore(headToken, context, offset, items, terminator);
                        return true;
                    default:
                        info.Content.Add(token);
                        break;
                }
                offset++;
            }
        }

        private static void AggregateCore(
            MarkdownHeadingBlockToken headToken,
            IMarkdownTokenAggregateContext context,
            int offset,
            List<DfmTabItemBlockToken> items,
            IMarkdownToken terminator)
        {
            var md = items.Aggregate(StringBuffer.Empty, (s, t) => s + t.SourceInfo.Markdown);
            if (terminator != null)
            {
                md += terminator.SourceInfo.Markdown;
                offset++;
            }
            var groupId = (items[0].SourceInfo.File ?? string.Empty).GetMd5String().Replace("/", "-").Remove(10);
            context.AggregateTo(
                new DfmTabGroupBlockToken(
                    DfmTabGroupBlockRule.Instance,
                    headToken.Context,
                    groupId,
                    items.ToImmutableArray(),
                    0,
                    headToken.SourceInfo.Copy(md.ToString())),
                offset);
        }

        private static DfmTabItemBlockToken CreateTabItem(
            TabItemInfo info)
        {
            var title = new DfmTabTitleBlockToken(
                DfmTabGroupBlockRule.Instance,
                info.Context,
                info.Title,
                info.HeadSource);
            var content = new DfmTabContentBlockToken(
                DfmTabGroupBlockRule.Instance,
                info.Context,
                info.Content.ToImmutableArray(),
                info.GetContentSourceInfo());
            return new DfmTabItemBlockToken(
                DfmTabGroupBlockRule.Instance,
                info.Context,
                info.Id,
                info.Condition,
                title,
                content,
                true,
                info.GetItemSourceInfo());
        }

        private static TabItemInfo ParseHeading(MarkdownHeadingBlockToken headToken)
        {
            if (headToken.Content.Tokens.Length == 1 &&
                headToken.Content.Tokens[0] is MarkdownLinkInlineToken link)
            {
                var m = HrefRegex.Match(link.Href);
                if (m.Success)
                {
                    return new TabItemInfo
                    {
                        Id = m.Groups["id"].Value,
                        Condition = m.Groups["condition"].Value,
                        Title = new InlineContent(link.Content),
                        HeadSource = headToken.SourceInfo,
                        Context = headToken.Context,
                    };
                }
            }
            return null;
        }

        private sealed class TabItemInfo
        {
            public string Id { get; set; }
            public string Condition { get; set; }
            public InlineContent Title { get; set; }
            public IMarkdownContext Context { get; set; }
            public SourceInfo HeadSource { get; set; }
            public List<IMarkdownToken> Content { get; } = new List<IMarkdownToken>();

            public SourceInfo GetContentSourceInfo() =>
                SourceInfo.Create(
                    Content.Aggregate(
                        StringBuffer.Empty,
                        (s, t) => s + t.SourceInfo.Markdown
                    ).ToString(),
                    HeadSource.File,
                    Content.FirstOrDefault()?.SourceInfo.LineNumber ?? HeadSource.LineNumber);

            public SourceInfo GetItemSourceInfo() =>
                HeadSource.Copy(
                    Content.Aggregate(
                        (StringBuffer)HeadSource.Markdown,
                        (s, t) => s + t.SourceInfo.Markdown
                    ).ToString());
        }
    }
}
