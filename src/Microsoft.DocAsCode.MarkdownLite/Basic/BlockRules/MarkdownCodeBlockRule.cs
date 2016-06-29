// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownCodeBlockRule : IMarkdownRule
    {
        public virtual string Name => "Code";

        public virtual Regex Code => Regexes.Block.Code;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Code.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var capStr = Regexes.Lexers.LeadingWhiteSpaces.Replace(match.Value, string.Empty);
            if (parser.Options.Pedantic)
            {
                return new MarkdownCodeBlockToken(this, parser.Context, capStr, null, sourceInfo);
            }
            else
            {
                return new MarkdownCodeBlockToken(this, parser.Context, Regexes.Lexers.TailingEmptyLines.Replace(capStr, string.Empty), null, sourceInfo);
            }
        }
    }
}
