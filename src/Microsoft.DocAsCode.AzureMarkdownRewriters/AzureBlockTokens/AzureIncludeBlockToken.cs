// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Immutable;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureIncludeBlockToken : AzureIncludeBasicToken, IMarkdownRewritable<AzureIncludeBlockToken>
    {
        public AzureIncludeBlockToken(IMarkdownRule rule, IMarkdownContext context, string src, string name, string title, ImmutableArray<IMarkdownToken> tokens, string raw, SourceInfo sourceInfo)
            : base(rule, context, src, name, title, tokens, raw, sourceInfo)
        {
        }

        public AzureIncludeBlockToken Rewrite(IMarkdownRewriteEngine rewriteEngine)
        {
            var tokens = rewriteEngine.Rewrite(Tokens);
            if (tokens == Tokens)
            {
                return this;
            }
            return new AzureIncludeBlockToken(Rule, Context, Src, Name, Title, tokens, Raw, SourceInfo);
        }
    }
}
