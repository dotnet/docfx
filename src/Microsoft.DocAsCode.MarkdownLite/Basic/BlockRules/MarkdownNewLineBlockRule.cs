// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownNewLineBlockRule : IMarkdownRule
    {
        public string Name => "NewLine";

        public virtual Regex Newline => Regexes.Block.Newline;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Newline.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);
            return new MarkdownNewLineBlockToken(this, parser.Context, match.Value, lineInfo);
        }
    }
}
