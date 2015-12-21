// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class MarkdownRewriterFactory
    {
        public static readonly IMarkdownRewriter Null = new MarkdownNullRewriter();

        public static IMarkdownRewriter FromLambda<TEngine, TToken>(
            Func<TEngine, TToken, IMarkdownToken> rewriteFunc)
            where TEngine : class, IMarkdownRewriteEngine
            where TToken : class, IMarkdownToken
        {
            if (rewriteFunc == null)
            {
                throw new ArgumentNullException(nameof(rewriteFunc));
            }
            return new MarkdownLambdaRewriter<TEngine, TToken>(rewriteFunc);
        }

        public static IMarkdownRewriter Composite(params IMarkdownRewriter[] rewriters)
        {
            return Composite((IEnumerable<IMarkdownRewriter>)rewriters);
        }

        public static IMarkdownRewriter Composite(IEnumerable<IMarkdownRewriter> rewriters)
        {
            if (rewriters == null)
            {
                throw new ArgumentNullException(nameof(rewriters));
            }
            return new MarkdownCompositeRewriter(rewriters.ToImmutableList());
        }

        public static IMarkdownRewriter Loop(IMarkdownRewriter rewriter, int maxLoopCount)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }
            if (maxLoopCount <= 0)
            {
                throw new ArgumentOutOfRangeException("Should be great than 0.", nameof(maxLoopCount));
            }
            return new MarkdownLoopRewriter(rewriter, maxLoopCount);
        }
    }
}
