// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
        public static SourceInfo? ToSourceInfo(this MarkdownObject obj, int? line = null, FilePath? file = null)
        {
            var path = file ?? (InclusionContext.File as Document)?.FilePath;
            if (path is null)
            {
                return default;
            }

            if (line != null)
            {
                return new SourceInfo(path, line.Value + 1, 0);
            }

            // Line info in markdown object is zero based, turn it into one based.
            return new SourceInfo(path, obj.Line + 1, obj.Column + 1);
        }

        public static SourceInfo? ToSourceInfo(this MarkdownObject obj, in HtmlTextRange html)
        {
            var path = (InclusionContext.File as Document)?.FilePath;
            if (path is null)
            {
                return default;
            }

            var start = OffSet(obj.Line, obj.Column, html.Start.Line, html.Start.Column);
            var end = OffSet(obj.Line, obj.Column, html.End.Line, html.End.Column);

            return new SourceInfo(path, start.line + 1, start.column + 1, end.line + 1, end.column + 1);

            static (int line, int column) OffSet(int line1, int column1, int line2, int column2)
            {
                return line2 == 0 ? (line1, column1 + column2) : (line1 + line2, column2);
            }
        }

        /// <summary>
        /// Traverse the markdown object graph, returns true to skip the current node.
        /// </summary>
        public static void Visit(this MarkdownObject? obj, Func<MarkdownObject, bool> action)
        {
            if (obj is null)
            {
                return;
            }

            if (action(obj))
            {
                return;
            }

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
