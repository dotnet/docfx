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
            return new TwoPhaseBlockToken(
                this,
                parser.Context,
                sourceInfo,
                (p, t) =>
                {
                    var capStr = LeadingBlockquote.Replace(t.SourceInfo.Markdown, string.Empty);
                    var blockTokens = p.Tokenize(t.SourceInfo.Copy(capStr));
                    blockTokens = TokenHelper.ParseInlineToken(p, t.Rule, blockTokens, true, t.SourceInfo);
                    return new MarkdownBlockquoteBlockToken(t.Rule, t.Context, blockTokens, t.SourceInfo);
                });
        }
    }
}
