// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTagInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Tag";

        public virtual Regex Tag => Regexes.Inline.Tag;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Tag.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            var c = parser.Context;
            var inLink = (bool)c.Variables[MarkdownInlineContext.IsInLink];
            if (!inLink && Regexes.Lexers.StartHtmlLink.IsMatch(match.Value))
            {
                parser.SwitchContext(MarkdownInlineContext.IsInLink, true);
            }
            else if (inLink && Regexes.Lexers.EndHtmlLink.IsMatch(match.Value))
            {
                parser.SwitchContext(MarkdownInlineContext.IsInLink, false);
            }
            return new MarkdownTagInlineToken(this, parser.Context, sourceInfo);
        }
    }
}
