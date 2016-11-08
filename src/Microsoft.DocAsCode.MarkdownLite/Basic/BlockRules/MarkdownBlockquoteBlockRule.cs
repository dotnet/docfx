// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Text.RegularExpressions;

    public class MarkdownBlockquoteBlockRule : IMarkdownRule
    {
        public virtual string Name => "Blockquote";

        public virtual Regex Blockquote => Regexes.Block.Blockquote;

        public virtual Regex LeadingBlockquote => Regexes.Lexers.LeadingBlockquote;

        public virtual IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Blockquote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var capStr = LeadingBlockquote.Replace(sourceInfo.Markdown, string.Empty);
            var blockTokens = parser.Tokenize(sourceInfo.Copy(capStr));
            blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, true, sourceInfo);
            return new MarkdownBlockquoteBlockToken(
                this,
                parser.Context,
                blockTokens,
                sourceInfo);
        }
    }
}
