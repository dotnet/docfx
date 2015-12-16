// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    public class InlineContent
    {
        public InlineContent(ImmutableArray<IMarkdownToken> tokens)
        {
            Tokens = tokens;
        }

        public ImmutableArray<IMarkdownToken> Tokens { get; }
    }
}
