// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    internal sealed class MarkdownLoopTokenRewriter : IMarkdownTokenRewriter, IInitializable
    {
        public MarkdownLoopTokenRewriter(IMarkdownTokenRewriter inner, int maxLoopCount)
        {
            Inner = inner;
            MaxLoopCount = maxLoopCount;
        }

        public IMarkdownTokenRewriter Inner { get; }

        public int MaxLoopCount { get; }

        public void Initialize(IMarkdownRewriteEngine rewriteEngine)
        {
            (Inner as IInitializable)?.Initialize(rewriteEngine);
        }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            IMarkdownToken lastToken;
            IMarkdownToken newToken = token;
            for (int loopCount = 0; loopCount < MaxLoopCount; loopCount++)
            {
                lastToken = newToken;
                newToken = Inner.Rewrite(engine, lastToken);
                if (newToken == null)
                {
                    return lastToken;
                }
            }
            throw new InvalidOperationException("Too many loops!");
        }
    }
}
