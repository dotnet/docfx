// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "AzureBlockquote";

        public override IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            var match = Blockquote.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            var capStr = LeadingBlockquote.Replace(match.Value, string.Empty);
            var c = engine.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
            var tokens = engine.Tokenize(sourceInfo.Copy(capStr));
            engine.SwitchContext(c);
            return new AzureBlockquoteBlockToken(this, engine.Context, tokens, sourceInfo);
        }
    }
}
