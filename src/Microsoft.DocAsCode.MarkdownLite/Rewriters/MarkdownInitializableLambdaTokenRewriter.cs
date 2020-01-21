// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    internal sealed class MarkdownInitializableLambdaTokenRewriter<TEngine, TToken>
        : IMarkdownTokenRewriter, IInitializable
        where TEngine : class, IMarkdownRewriteEngine
        where TToken : class, IMarkdownToken
    {
        public MarkdownInitializableLambdaTokenRewriter(
            Func<TEngine, TToken, IMarkdownToken> rewriteFunc,
            Action<TEngine> initializer)
        {
            RewriteFunc = rewriteFunc;
            Initializer = initializer;
        }

        public Func<TEngine, TToken, IMarkdownToken> RewriteFunc { get; }

        public Action<TEngine> Initializer { get; }

        public void Initialize(IMarkdownRewriteEngine rewriteEngine)
        {
            if (rewriteEngine is TEngine tengine)
            {
                Initializer(tengine);
            }
        }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            if (engine is TEngine tengine && token is TToken ttoken)
            {
                return RewriteFunc(tengine, ttoken);
            }
            return null;
        }
    }
}
