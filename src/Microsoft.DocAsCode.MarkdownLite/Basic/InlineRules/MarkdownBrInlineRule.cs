// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownBrInlineRule : IMarkdownRule
    {
        public string Name => "Inline.Br";

        public virtual Regex Br => Regexes.Inline.Br;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Br.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            return new MarkdownBrInlineToken(this, parser.Context, match.Value, lineInfo);
        }
    }
}
