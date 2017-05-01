// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;

    using Microsoft.DocAsCode.MarkdownLite;

    public abstract class PlugableRendererBase : IDisposable
    {
        private readonly object _innerRenderer;

        public PlugableRendererBase(object innerRenderer)
        {
            _innerRenderer = innerRenderer;
        }

        public StringBuffer Render(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
        {
            return BaseRender(renderer, token, context);
        }

        public StringBuffer BaseRender(IMarkdownRenderer renderer, IMarkdownToken token, IMarkdownContext context)
        {
            // double dispatch.
            return ((dynamic)_innerRenderer).Render((dynamic)renderer, (dynamic)token, (dynamic)context);
        }

        public virtual void Dispose()
        {
            (_innerRenderer as IDisposable)?.Dispose();
        }
    }
}
