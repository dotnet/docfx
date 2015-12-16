// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    internal sealed class MarkdownLoopRewriter : IMarkdownRewriter
    {
        public MarkdownLoopRewriter(IMarkdownRewriter inner, int maxLoopCount)
        {
            Inner = inner;
            MaxLoopCount = maxLoopCount;
        }

        public IMarkdownRewriter Inner { get; }

        public int MaxLoopCount { get; }

        public IMarkdownToken Rewrite(MarkdownParser engine, IMarkdownToken token)
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
