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
        public static string? GetZone(this MarkdownObject obj)
        {
            foreach (var item in obj.GetPathToRootInclusive())
            {
                if (item is TripleColonBlock block && block.Extension is ZoneExtension)
                {
                    var properties = block.TryGetAttributes()?.Properties;
                    if (properties is null)
                    {
                        return null;
                    }

                    return properties.FirstOrDefault(p => p.Key == "data-target").Value ??
                          (properties.Any(p => p.Key == "data-pivot") ? "pivot" : null);
                }
            }

            return null;
        }

        public static MonikerList GetZoneLevelMonikers(this MarkdownObject obj)
        {
            foreach (var item in obj.GetPathToRootInclusive())
            {
                if (item is MonikerRangeBlock block)
                {
                    return block.ParsedMonikers is MonikerList list ? list : default;
                }
            }

            return default;
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
