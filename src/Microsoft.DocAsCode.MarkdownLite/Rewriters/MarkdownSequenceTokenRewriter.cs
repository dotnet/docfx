// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Immutable;

    internal sealed class MarkdownSequenceTokenRewriter : IMarkdownTokenRewriter, IInitializable
    {
        public MarkdownSequenceTokenRewriter(ImmutableArray<IMarkdownTokenRewriter> inner)
        {
            Inner = inner;
        }

        public ImmutableArray<IMarkdownTokenRewriter> Inner { get; }

        public void Initialize(IMarkdownRewriteEngine rewriteEngine)
        {
            foreach (var item in Inner)
            {
                (item as IInitializable)?.Initialize(rewriteEngine);
            }
        }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            IMarkdownToken newToken = token;
            for (int index = 0; index < Inner.Length; index++)
            {
                newToken = Inner[index].Rewrite(engine, newToken) ?? newToken;
            }
            return newToken;
        }
    }
}
