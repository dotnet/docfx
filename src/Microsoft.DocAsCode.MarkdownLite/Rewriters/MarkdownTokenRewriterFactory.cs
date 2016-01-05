// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public static class MarkdownTokenRewriterFactory
    {
        public static readonly IMarkdownTokenRewriter Null = new MarkdownNullTokenRewriter();

        public static IMarkdownTokenRewriter FromLambda<TEngine, TToken>(
            Func<TEngine, TToken, IMarkdownToken> rewriteFunc)
            where TEngine : class, IMarkdownRewriteEngine
            where TToken : class, IMarkdownToken
        {
            if (rewriteFunc == null)
            {
                throw new ArgumentNullException(nameof(rewriteFunc));
            }
            return new MarkdownLambdaTokenRewriter<TEngine, TToken>(rewriteFunc);
        }

        public static IMarkdownTokenRewriter Composite(params IMarkdownTokenRewriter[] rewriters)
        {
            return Composite((IEnumerable<IMarkdownTokenRewriter>)rewriters);
        }

        public static IMarkdownTokenRewriter Composite(IEnumerable<IMarkdownTokenRewriter> rewriters)
        {
            if (rewriters == null)
            {
                throw new ArgumentNullException(nameof(rewriters));
            }
            return new MarkdownCompositeTokenRewriter(rewriters.ToImmutableList());
        }

        public static IMarkdownTokenRewriter Loop(IMarkdownTokenRewriter rewriter, int maxLoopCount)
        {
            if (rewriter == null)
            {
                throw new ArgumentNullException(nameof(rewriter));
            }
            if (maxLoopCount <= 0)
            {
                throw new ArgumentOutOfRangeException("Should be great than 0.", nameof(maxLoopCount));
            }
            return new MarkdownLoopTokenRewriter(rewriter, maxLoopCount);
        }
    }
}
