// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Collections.Immutable;

    using MarkdownLite;

    public class DfmBlockquoteBlockToken : MarkdownBlockquoteBlockToken
    {
        public DfmBlockquoteBlockToken(IMarkdownRule rule, ImmutableArray<IMarkdownToken> tokens)
            : base(rule, tokens)
        {
        }
    }
}
