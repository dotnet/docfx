﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureBlockquoteBlockRule : MarkdownBlockquoteBlockRule
    {
        public override string Name => "AzureBlockquote";

        public override IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
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
            return new AzureBlockquoteBlockToken(this, engine.Context, tokens, match.Value);
        }
    }
}
