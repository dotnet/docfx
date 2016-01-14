// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;

    internal sealed class MarkdownLambdaTokenRewriter<TEngine, TToken> : IMarkdownTokenRewriter
        where TEngine : class, IMarkdownRewriteEngine
        where TToken : class, IMarkdownToken
    {
        public MarkdownLambdaTokenRewriter(Func<TEngine, TToken, IMarkdownToken> rewriteFunc)
        {
            RewriteFunc = rewriteFunc;
        }

        public Func<TEngine, TToken, IMarkdownToken> RewriteFunc { get; }

        public IMarkdownToken Rewrite(IMarkdownRewriteEngine engine, IMarkdownToken token)
        {
            var tengine = engine as TEngine;
            var ttoken = token as TToken;
            if (tengine != null && ttoken != null)
            {
                return RewriteFunc(tengine, ttoken);
            }
            return null;
        }
    }
}
