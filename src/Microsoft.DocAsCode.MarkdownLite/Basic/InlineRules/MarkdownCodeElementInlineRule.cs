// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeElementInlineRule : IMarkdownRule
    {
        public string Name => "Inline.CodeElement";

        public virtual Regex CodeElement => Regexes.Inline.CodeElement;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = CodeElement.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);

            return new MarkdownRawToken(this, parser.Context, match.Value, lineInfo);
        }
    }
}
