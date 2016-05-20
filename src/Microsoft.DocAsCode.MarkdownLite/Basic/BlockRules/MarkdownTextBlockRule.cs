// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownTextBlockRule : IMarkdownRule
    {
        public string Name => "Text";

        public virtual Regex Text => Regexes.Block.Text;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParserContext context)
        {
            if ((bool)parser.Context.Variables[MarkdownBlockContext.IsTop])
            {
                return null;
            }
            var match = Text.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var lineInfo = context.LineInfo;
            context.Consume(match.Length);
            return new MarkdownTextToken(this, parser.Context, match.Value, match.Value, lineInfo);
        }
    }
}
