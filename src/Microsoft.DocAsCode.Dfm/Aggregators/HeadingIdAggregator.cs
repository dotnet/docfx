// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class HeadingIdAggregator : MarkdownTokenAggregator<MarkdownHeadingBlockToken>
    {
        private static readonly Regex OpenARegex = new Regex(@"^\<a +(?:name|id)=\""([\w \-\.]+)\"" *\>$", RegexOptions.Compiled);
        private static readonly Regex CloseARegex = new Regex(@"^\<\/a\>$", RegexOptions.Compiled);

        protected override bool AggregateCore(MarkdownHeadingBlockToken headToken, IMarkdownTokenAggregateContext context)
        {
            var info = ParseHeading(headToken);
            if (info == null)
            {
                return false;
            }
            context.AggregateTo(
                new MarkdownHeadingBlockToken(
                    headToken.Rule,
                    headToken.Context,
                    new InlineContent(headToken.Content.Tokens.RemoveRange(0, 2)),
                    info,
                    headToken.Depth,
                    headToken.SourceInfo), 1);
            return true;
        }

        private static string ParseHeading(MarkdownHeadingBlockToken headToken)
        {
            if (headToken.Content.Tokens.Length <= 2)
            {
                return null;
            }
            var openATag = headToken.Content.Tokens[0] as MarkdownTagInlineToken;
            var closeATag = headToken.Content.Tokens[1] as MarkdownTagInlineToken;
            if (openATag == null || closeATag == null)
            {
                return null;
            }

            var m = OpenARegex.Match(openATag.SourceInfo.Markdown);
            if (!m.Success)
            {
                return null;
            }
            if (!CloseARegex.IsMatch(closeATag.SourceInfo.Markdown))
            {
                return null;
            }

            return m.Groups[1].Value;
        }
    }
}
