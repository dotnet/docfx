// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownHrBlockRule : IMarkdownRule
    {
        public string Name => "Hr";

        public virtual Regex Hr => Regexes.Block.Hr;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            var match = Hr.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo= context.LineInfo;
            context.Consume(match.Length);
            return new MarkdownHrBlockToken(this, parser.Context, match.Value, lineInfo);
        }
    }
}
