// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEscapedTextInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.EscapedText";

        public virtual Regex EscapedText => Regexes.Inline.EscapedText;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = EscapedText.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownTextToken(this, parser.Context, StringHelper.Escape(match.Groups[1].Value), sourceInfo);
        }
    }
}
