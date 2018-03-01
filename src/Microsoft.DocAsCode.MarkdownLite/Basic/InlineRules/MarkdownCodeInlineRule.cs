// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeInlineRule : IMarkdownRule
    {
        public virtual string Name => "Inline.Code";

        public virtual Regex Code => Regexes.Inline.Code;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Code.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new MarkdownCodeInlineToken(this, parser.Context, match.Groups[2].Value, sourceInfo);
        }
    }
}
