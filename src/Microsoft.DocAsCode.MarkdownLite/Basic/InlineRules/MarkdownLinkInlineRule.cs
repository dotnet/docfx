// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownLinkInlineRule : MarkdownLinkBaseInlineRule
    {
        public override string Name => "Inline.Link";

        public virtual Regex Link => Regexes.Inline.Link;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Link.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            return GenerateToken(parser, match.Groups[2].Value, match.Groups[4].Value, match.Groups[1].Value, match.Value[0] == '!', match.Value, lineInfo);
        }
    }
}
