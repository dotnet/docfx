// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Collections.Immutable;

    using MarkdownLite;

    public class AzureBlockquoteBlockToken : MarkdownBlockquoteBlockToken
    {
        public AzureBlockquoteBlockToken(IMarkdownRule rule, IMarkdownContext context, ImmutableArray<IMarkdownToken> tokens, SourceInfo sourceInfo)
            : base(rule, context, tokens, sourceInfo)
        {
        }
    }
}
