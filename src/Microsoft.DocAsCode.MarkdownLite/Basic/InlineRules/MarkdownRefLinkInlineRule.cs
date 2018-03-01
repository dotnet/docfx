// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownRefLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.RefLink";

        public virtual Regex RefLink => Regexes.Inline.RefLink;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = RefLink.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            if (MarkdownInlineContext.GetIsInLink(parser.Context) && match.Value[0] != '!')
            {
                return null;
            }

            var linkStr = match.NotEmpty(2, 1).ReplaceRegex(Regexes.Lexers.WhiteSpaces, " ");

            parser.Links.TryGetValue(linkStr.ToLower(), out LinkObj link);

            if (string.IsNullOrEmpty(link?.Href))
            {
                var sourceInfo = context.Consume(1);
                var text = match.Value.Remove(1);
                return new MarkdownTextToken(this, parser.Context, text, sourceInfo);
            }
            else
            {
                var sourceInfo = context.Consume(match.Length);
                return GenerateToken(parser, link.Href, link.Title, match.Groups[1].Value, match.Value[0] == '!', sourceInfo, MarkdownLinkType.RefLink, linkStr);
            }
        }
    }
}
