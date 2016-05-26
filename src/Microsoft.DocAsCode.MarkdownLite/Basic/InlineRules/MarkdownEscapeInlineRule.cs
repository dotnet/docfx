// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownEscapeInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Escape";

        public virtual Regex Escape => Regexes.Inline.Escape;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Escape.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownEscapeInlineToken(this, parser.Context, match.Groups[1].Value, sourceInfo);
        }
    }
}
