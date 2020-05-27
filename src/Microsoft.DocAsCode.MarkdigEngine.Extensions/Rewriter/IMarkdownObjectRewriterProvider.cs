// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Collections.Immutable;


    public interface IMarkdownObjectRewriterProvider
    {
        ImmutableArray<IMarkdownObjectRewriter> GetRewriters();
    }
}
