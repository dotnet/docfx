// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownStrongInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Strong";

        public virtual Regex Strong => Regexes.Inline.Strong;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Strong.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            return new MarkdownStrongInlineToken(this, parser.Context, parser.Tokenize(match.NotEmpty(2, 1), lineInfo), match.Value, lineInfo);
        }
    }
}
