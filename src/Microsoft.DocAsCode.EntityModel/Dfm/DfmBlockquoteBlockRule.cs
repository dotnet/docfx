// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using MarkdownLite;

    public class DfmBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "DfmBlockquote";

        public override IMarkdownToken TryMatch(MarkdownParser engine, ref string source)
        {
            var match = Blockquote.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            var capStr = LeadingBlockquote.Replace(match.Value, string.Empty);
            var c = engine.SwitchContext(MarkdownBlockContext.IsBlockQuote, true);
            var tokens = engine.Tokenize(capStr);
            engine.SwitchContext(c);
            return new DfmBlockquoteBlockToken(this, engine.Context, tokens);
        }
    }
}
