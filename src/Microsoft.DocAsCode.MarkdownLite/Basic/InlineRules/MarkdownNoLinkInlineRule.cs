// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownNoLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.NoLink";

        public virtual Regex NoLink => Regexes.Inline.NoLink;

        public override IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = NoLink.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            var linkStr = match.NotEmpty(2, 1).ReplaceRegex(Regexes.Lexers.WhiteSpaces, " ");

            LinkObj link;
            engine.Links.TryGetValue(linkStr.ToLower(), out link);

            if (string.IsNullOrEmpty(link?.Href))
            {
                source = match.Groups[0].Value.Substring(1) + source;
                return new MarkdownTextToken(this, engine.Context, match.Groups[0].Value[0].ToString());
            }
            return GenerateToken(engine, link.Href, link.Title, match.Groups[1].Value, match.Value[0] == '!');
        }
    }
}
