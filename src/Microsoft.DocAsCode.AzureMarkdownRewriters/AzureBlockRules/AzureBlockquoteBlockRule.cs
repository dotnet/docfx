// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "AzureBlockquote";

        public static readonly Regex _azureLeadingBlankRegex = new Regex(@"^ *>? *", RegexOptions.Multiline | RegexOptions.Compiled);

        public override Regex LeadingBlockquote => _azureLeadingBlankRegex;

        public override IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = Blockquote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var c = parser.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
            var capStr = LeadingBlockquote.Replace(sourceInfo.Markdown, string.Empty);
            var blockTokens = parser.Tokenize(sourceInfo.Copy(capStr));
            blockTokens = TokenHelper.CreateParagraghs(parser, this, blockTokens, true, sourceInfo);
            parser.SwitchContext(c);
            return new MarkdownBlockquoteBlockToken(
                this,
                parser.Context,
                blockTokens,
                sourceInfo);
        }
    }
}
