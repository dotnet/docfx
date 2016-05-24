// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System;
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureIncludeInlineToken : AzureIncludeBasicToken, IMarkdownRewritable<AzureIncludeInlineToken>
    {
        public AzureIncludeInlineToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, ImmutableArray<IMarkdownToken> tokens, string raw, SourceInfo sourceInfo)
            : base(rule, context, src, name, title, tokens, raw, sourceInfo)
        {
        }

        public AzureIncludeInlineToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var tokens = rewriteEngine.Rewrite(Tokens);
            if (tokens == Tokens)
            {
                return this;
            }
            return new AzureIncludeInlineToken(Rule, Context, Src, Name, Title, tokens, Raw, SourceInfo);
        }
    }
}
