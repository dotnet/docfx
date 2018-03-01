// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class DfmCustomizedRendererPartBase<TRenderer, TToken, TContext> : IDfmCustomizedRendererPart
        where TRenderer : IMarkdownRenderer
        where TToken : IMarkdownToken
        where TContext : IMarkdownContext
    {
        protected DfmCustomizedRendererPartBase() { }

        public abstract string Name { get; }
        public Type MarkdownRendererType => typeof(TRenderer);
        public Type MarkdownTokenType => typeof(TToken);
        public Type MarkdownContextType => typeof(TContext);

        bool IDfmCustomizedRendererPart.Match(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
        {
            return Match((TRenderer)renderer, (TToken)token, (TContext)context);
        }

        StringBuffer IDfmCustomizedRendererPart.Render(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
        {
            return Render((TRenderer)renderer, (TToken)token, (TContext)context);
        }

        public abstract bool Match(TRenderer renderer, TToken token, TContext context);

        public abstract StringBuffer Render(TRenderer renderer, TToken token, TContext context);
    }
}
