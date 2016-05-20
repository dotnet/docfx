// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;
    using System.Text.RegularExpressions;

    public class GfmHtmlCommentBlockRule : IMarkdownRule
    {
        public virtual string Name => "GfmHtmlComment";

        public virtual Regex HtmlComment => Regexes.Block.Gfm.HtmlComment;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
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
