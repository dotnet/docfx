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

            var linkStr = match.NotEmpty(2, 1).ReplaceRegex(Regexes.Lexers.WhiteSpaces, " ");

            LinkObj link;
            parser.Links.TryGetValue(linkStr.ToLower(), out link);

            if (string.IsNullOrEmpty(link?.Href))
            {
                var lineInfo = context.Consume(1);
                var text = match.Value.Remove(1);
                return new MarkdownTextToken(this, parser.Context, text, lineInfo);
            }
            else
            {
                var lineInfo = context.Consume(match.Length);
                return GenerateToken(parser, link.Href, link.Title, match.Groups[1].Value, match.Value[0] == '!', lineInfo);
            }
        }
    }
}
