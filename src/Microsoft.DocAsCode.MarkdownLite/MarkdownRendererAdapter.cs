// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using CSharp.RuntimeBinder;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The adapter for markdown renderer, use dynamic dispatch.
    /// </summary>
    public class MarkdownRendererAdapter : IMarkdownRenderer
    {
        public MarkdownRendererAdapter(IMarkdownEngine engine, object renderer, Options options, Dictionary<string, LinkObj> links)
        {
            Engine = engine;
            Renderer = renderer;
            Options = options;
            Links = links;
        }

        public IMarkdownEngine Engine { get; }

        public object Renderer { get; }

        public Options Options { get; }

        public Dictionary<string, LinkObj> Links { get; }

        public StringBuffer Render(IMarkdownToken token)
        {
            try
            {
                // double dispatch.
                return ((dynamic)Renderer).Render((dynamic)this, (dynamic)token, (dynamic)token.Context);
            }
            catch (RuntimeBinderException ex)
            {
                throw new InvalidOperationException($"Unable to handle token: {token.GetType().Name}, rule: {token.Rule.Name}", ex);
            }
        }
    }
}
