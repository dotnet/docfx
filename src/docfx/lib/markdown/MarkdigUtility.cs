// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
        /// <summary>
        /// Traverse the markdown object graph, returns true to stop the traversal.
        /// </summary>
        public static bool Visit(this MarkdownObject obj, Func<MarkdownObject, bool> action)
        {
            if (obj == null)
                return true;

            if (action(obj))
                return true;

            if (obj is ContainerBlock block)
            {
                foreach (var child in block)
                {
                    if (Visit(child, action))
                    {
                        return true;
                    }
                }
            }
            else if (obj is LeafBlock leaf)
            {
                if (leaf.Inline != null)
                {
                    foreach (var child in leaf.Inline)
                    {
                        if (Visit(child, action))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    if (Visit(child, action))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed)
        {
            builder.Extensions.Add(new DelegatingExtension(pipeline => pipeline.DocumentProcessed += documentProcessed));
            return builder;
        }

        public static MarkdownPipelineBuilder Use<T>(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed, Action<HtmlRenderer, T> render)
             where T : MarkdownObject
        {
            builder.Extensions.Add(
                new DelegatingExtension(
                    pipeline => pipeline.DocumentProcessed += documentProcessed,
                    renderer => renderer.ObjectRenderers.Add(new DelegatingRenderer<T>(render))));
            return builder;
        }

        private class DelegatingExtension : IMarkdownExtension
        {
            private readonly Action<MarkdownPipelineBuilder> _setupPipeline;
            private readonly Action<IMarkdownRenderer> _setupRenderer;

            public DelegatingExtension(Action<MarkdownPipelineBuilder> setupPipeline = null, Action<IMarkdownRenderer> setupRenderer = null)
            {
                _setupPipeline = setupPipeline;
                _setupRenderer = setupRenderer;
            }

            public void Setup(MarkdownPipelineBuilder pipeline) => _setupPipeline?.Invoke(pipeline);

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => _setupRenderer?.Invoke(renderer);
        }

        private class DelegatingRenderer<T> : HtmlObjectRenderer<T> where T : MarkdownObject
        {
            private readonly Action<HtmlRenderer, T> _render;

            public DelegatingRenderer(Action<HtmlRenderer, T> render) => _render = render;

            protected override void Write(HtmlRenderer renderer, T obj) => _render(renderer, obj);
        }
    }
}
