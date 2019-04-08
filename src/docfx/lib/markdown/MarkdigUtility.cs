// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
        public static Range ToRange(this MarkdownObject obj, int? line = null)
        {
            // Line info in markdown object is zero based, turn it into one based.
            if (obj != null)
                return new Range(obj.Line + 1, obj.Column + 1);

            if (line != null)
                return new Range(line.Value + 1, 0);

            return default;
        }

        /// <summary>
        /// Traverse the markdown object graph, returns true to skip the current node.
        /// </summary>
        public static void Visit(this MarkdownObject obj, Func<MarkdownObject, bool> action)
        {
            if (obj is null)
                return;

            if (action(obj))
                return;

            if (obj is ContainerBlock block)
            {
                foreach (var child in block)
                {
                    Visit(child, action);
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    Visit(child, action);
                }
            }
            else if (obj is LeafBlock leaf)
            {
                Visit(leaf.Inline, action);
            }
        }

        /// <summary>
        /// Traverses the markdown object graph and replace each node with another node,
        /// If <paramref name="action"/> returns null, remove the node from the graph.
        /// </summary>
        public static MarkdownObject Replace(this MarkdownObject obj, Func<MarkdownObject, MarkdownObject> action)
        {
            if (obj is null)
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
                    if (replacement is null)
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
