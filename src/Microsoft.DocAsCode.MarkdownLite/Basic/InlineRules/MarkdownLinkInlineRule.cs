// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.Link";

        public virtual Regex Link => Regexes.Inline.Link;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Link.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            if (MarkdownInlineContext.GetIsInLink(parser.Context) && match.Value[0] != '!')
            {
                return null;
            }
            if (IsEscape(match.Groups[1].Value) || IsEscape(match.Groups[2].Value))
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return GenerateToken(parser, match.Groups[2].Value, match.Groups[4].Value, match.Groups[1].Value, match.Value[0] == '!', sourceInfo, MarkdownLinkType.NormalLink, null);
        }

        private bool IsEscape(string text)
        {
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] != '\\')
                {
                    return (text.Length - i) % 2 == 0;
                }
            }
            return text.Length % 2 == 1;
        }
    }
}
