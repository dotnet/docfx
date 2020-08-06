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

namespace Microsoft.Docs.Build
{
    internal static class MarkdigUtility
    {
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
        public static void Visit(this MarkdownObject? obj, MarkdownVisitContext context, Func<MarkdownObject, MarkdownVisitContext, bool> action)
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
                    var monikers = monikerRangeBlock.GetAttributes()
                                                    .Properties.First(p => p.Key == "data-moniker")
                                                    .Value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
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

                case TripleColonBlock tripleColonBlock when tripleColonBlock.Extension is ZoneExtension:
                    string? target = null;
                    if (tripleColonBlock.GetAttributes().Properties.Any(p => p.Key == "data-target"))
                    {
                        target = tripleColonBlock.GetAttributes().Properties.FirstOrDefault(p => p.Key == "data-target").Value;
                    }
                    else if (tripleColonBlock.GetAttributes().Properties.Any(p => p.Key == "data-pivot"))
                    {
                        target = "pivot";
                    }
                    if (!string.IsNullOrEmpty(target))
                    {
                        context.ZoneStack.Push(target);
                    }
                    foreach (var child in tripleColonBlock)
                    {
                        Visit(child, context, action);
                    }
                    if (!string.IsNullOrEmpty(target))
                    {
                        context.ZoneStack.Pop();
                    }
                    break;

                case TripleColonBlock tripleColonBlock:
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
                    HtmlBlock htmlBlock => HtmlUtility.IsVisible(htmlBlock.Lines.ToString()),
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
