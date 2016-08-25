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

        public override IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            var match = Blockquote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new TwoPhaseBlockToken(
                this,
                engine.Context,
                sourceInfo,
                (p, t) =>
                {
                    var capStr = LeadingBlockquote.Replace(t.SourceInfo.Markdown, string.Empty);
                    var c = p.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
                    var tokens = p.Tokenize(t.SourceInfo.Copy(capStr));
                    tokens = TokenHelper.ParseInlineToken(p, t.Rule, tokens, true, t.SourceInfo);
                    p.SwitchContext(c);
                    return new AzureBlockquoteBlockToken(this, t.Context, tokens, t.SourceInfo);
                });
        }
    }
}
