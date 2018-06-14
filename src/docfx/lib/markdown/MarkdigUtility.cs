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
            else if (obj is LeafBlock leaf)
            {
                Visit(leaf.Inline, action);
            }

            return false;
        }

        /// <summary>
        /// Traverses the markdown object graph and replace each node with another node,
        /// If <paramref name="action"/> returns null, remove the node from the graph.
        /// </summary>
        public static MarkdownObject Replace(this MarkdownObject obj, Func<MarkdownObject, MarkdownObject> action)
        {
            if (obj == null)
                return null;

            obj = action(obj);

            if (obj is ContainerBlock block)
            {
                for (var i = 0; i < block.Count; i++)
                {
                    var replacement = (Block)Replace(block[i], action);
                    if (replacement != block[i])
                    {
                        block.RemoveAt(i--);
                        if (replacement != null)
                        {
                            block.Insert(i, replacement);
                        }
                    }
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    var replacement = Replace(child, action);
                    if (replacement == null)
                    {
                        child.Remove();
                    }
                    else if (replacement != child)
                    {
                        child.ReplaceBy((Inline)replacement);
                    }
                }
            }
            else if (obj is LeafBlock leaf)
            {
                leaf.Inline = (ContainerInline)Replace(leaf.Inline, action);
            }

            return obj;
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed)
        {
            builder.Extensions.Add(new DelegatingExtension(pipeline => pipeline.DocumentProcessed += documentProcessed));
            return builder;
        }

        private class DelegatingExtension : IMarkdownExtension
        {
            private readonly Action<MarkdownPipelineBuilder> _setupPipeline;

            public DelegatingExtension(Action<MarkdownPipelineBuilder> setupPipeline)
            {
                _setupPipeline = setupPipeline;
            }

            public void Setup(MarkdownPipelineBuilder pipeline) => _setupPipeline?.Invoke(pipeline);

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
            {
            }
        }
    }
}
