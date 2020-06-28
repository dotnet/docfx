// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

#pragma warning disable CS0618

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
        private static readonly object s_filePathKey = new object();

        public static Document GetFilePath(this MarkdownObject? obj)
        {
            while (true)
            {
                switch (obj)
                {
                    case Block block when block.Parent is null || block.Parent is InclusionBlock:
                    case Inline inline when inline.Parent is null || inline.Parent is InclusionInline:
                        return obj.GetData(s_filePathKey) as Document ?? (Document)InclusionContext.File;

                    case Block block:
                        obj = block.Parent;
                        break;

                    case Inline inline:
                        obj = inline.Parent;
                        break;

                    default:
                        return (Document)InclusionContext.File;
                }
            }
        }

        public static void SetFilePath(this MarkdownObject obj, Document value)
        {
            obj.SetData(s_filePathKey, value);
        }

        public static SourceInfo? GetSourceInfo(this MarkdownObject? obj, int? line = null)
        {
            var path = GetFilePath(obj).FilePath;

            if (line != null)
            {
                return new SourceInfo(path, line.Value + 1, 0);
            }

            if (obj is null)
            {
                return new SourceInfo(path, 0, 0);
            }

            // Line info in markdown object is zero based, turn it into one based.
            return new SourceInfo(path, obj.Line + 1, obj.Column + 1);
        }

        public static SourceInfo? GetSourceInfo(this MarkdownObject obj, in HtmlTextRange html)
        {
            var path = GetFilePath(obj).FilePath;

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
        /// Traverse the markdown object graph, returns true to skip the current node.
        /// </summary>
        public static void Visit(
            this MarkdownObject? obj, MarkdownVisitContext context, Func<MarkdownObject, MarkdownVisitContext, bool> action)
        {
            if (obj is null)
            {
                return;
            }

            if (action(obj, context))
            {
                return;
            }

            switch (obj)
            {
                case MonikerRangeBlock monikerRangeBlock:
                    var monikers = monikerRangeBlock.GetAttributes().Properties.First(p => p.Key == "data-moniker").Value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    context.ZoneMonikerStack.Push(new MonikerList(monikers));
                    foreach (var child in monikerRangeBlock)
                    {
                        Visit(child, context, action);
                    }
                    context.ZoneMonikerStack.Pop();
                    break;

                case InclusionBlock inclusionBlock:
                    context.FileStack.Push(new SourceInfo<Document>((Document)inclusionBlock.ResolvedFilePath, inclusionBlock.GetSourceInfo()));
                    foreach (var child in inclusionBlock)
                    {
                        Visit(child, context, action);
                    }
                    context.FileStack.Pop();
                    break;

                case ContainerBlock block:
                    foreach (var child in block)
                    {
                        Visit(child, context, action);
                    }
                    break;

                case InclusionInline inclusionInline:
                    context.FileStack.Push(new SourceInfo<Document>((Document)inclusionInline.ResolvedFilePath, inclusionInline.GetSourceInfo()));
                    foreach (var child in inclusionInline)
                    {
                        Visit(child, context, action);
                    }
                    context.FileStack.Pop();
                    break;

                case ContainerInline inline:
                    foreach (var child in inline)
                    {
                        Visit(child, context, action);
                    }
                    break;

                case LeafBlock leaf:
                    Visit(leaf.Inline, context, action);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Traverses the markdown object graph and replace each node with another node,
        /// If <paramref name="action"/> returns null, remove the node from the graph.
        /// </summary>
        public static MarkdownObject Replace(this MarkdownObject obj, Func<MarkdownObject, MarkdownObject?> action)
        {
            return ReplaceCore(obj, action) ?? new MarkdownDocument();

            static MarkdownObject? ReplaceCore(MarkdownObject obj, Func<MarkdownObject, MarkdownObject?> action)
            {
                switch (action(obj))
                {
                    case null:
                        return null;

                    case ContainerBlock block:
                        for (var i = 0; i < block.Count; i++)
                        {
                            var replacement = ReplaceCore(block[i], action) as Block;
                            if (replacement != block[i])
                            {
                                block.RemoveAt(i--);
                                if (replacement != null)
                                {
                                    block.Insert(i, replacement);
                                }
                            }
                        }
                        return block;

                    case ContainerInline inline:
                        foreach (var child in inline)
                        {
                            var replacement = ReplaceCore(child, action) as Inline;
                            if (replacement is null)
                            {
                                child.Remove();
                            }
                            else if (replacement != child)
                            {
                                child.ReplaceBy(replacement);
                            }
                        }
                        return inline;

                    case LeafBlock leaf:
                        leaf.Inline = ReplaceCore(leaf.Inline, action) as ContainerInline;
                        return leaf;

                    case MarkdownObject other:
                        return other;
                }
            }
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, Action<IMarkdownRenderer>? setupRenderer)
        {
            builder.Extensions.Add(new DelegatingExtension(null, setupRenderer));
            return builder;
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, Action<MarkdownPipelineBuilder> setupPipeline)
        {
            builder.Extensions.Add(new DelegatingExtension(setupPipeline, null));
            return builder;
        }

        public static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed)
        {
            builder.Extensions.Add(new DelegatingExtension(pipeline => pipeline.DocumentProcessed += documentProcessed, null));
            return builder;
        }

        public static bool IsVisible(this MarkdownObject markdownObject)
        {
            var visible = false;

            Visit(markdownObject, node =>
            {
                var nodeVisible = node switch
                {
                    HtmlBlock htmlBlock => htmlBlock.Lines.Lines.Any(line => HtmlUtility.IsVisible(line.Slice.ToString())),
                    HtmlInline htmlInline => HtmlUtility.IsVisible(htmlInline.Tag),
                    LinkReferenceDefinition _ => false,
                    ThematicBreakBlock _ => false,
                    YamlFrontMatterBlock _ => false,
                    HeadingBlock headingBlock when headingBlock.Inline is null || !headingBlock.Inline.Any() => false,
                    LeafBlock leafBlock when leafBlock.Inline is null || !leafBlock.Inline.Any() => true,
                    LeafInline _ => true,
                    _ => false,
                };

                return visible = nodeVisible || visible;
            });

            return visible;
        }

        private class DelegatingExtension : IMarkdownExtension
        {
            private readonly Action<MarkdownPipelineBuilder>? _setupPipeline;
            private readonly Action<IMarkdownRenderer>? _setupRenderer;

            public DelegatingExtension(Action<MarkdownPipelineBuilder>? setupPipeline, Action<IMarkdownRenderer>? setupRenderer)
            {
                _setupPipeline = setupPipeline;
                _setupRenderer = setupRenderer;
            }

            public void Setup(MarkdownPipelineBuilder pipeline) => _setupPipeline?.Invoke(pipeline);

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => _setupRenderer?.Invoke(renderer);
        }
    }
}
