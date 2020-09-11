// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using HtmlReaderWriter;
using Markdig;
using Markdig.Extensions.Tables;
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

        public static bool IsInlineImage(this MarkdownObject node, SourceInfo<string> source)
        {
            switch (node)
            {
                case Inline inline:
                    return inline.IsInlineImage();
                case HtmlBlock htmlBlock:
                    if (htmlBlock.IsInlineImage(source))
                    {
                        Console.WriteLine($"HtmlBlockInlineImage: {source.Source!.File} {source.Source!.Line}:{source.Source!.Column}-" +
                            $"{source.Source!.EndLine}:{source.Source!.EndColumn}");
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        private static bool IsInlineImage(this Inline node)
        {
            switch (node)
            {
                case LinkInline linkInline when linkInline.IsImage:
                case TripleColonInline tripleColonInline when tripleColonInline.Extension is ImageExtension:
                case HtmlInline htmlInline when htmlInline.Tag.StartsWith("<img", StringComparison.InvariantCultureIgnoreCase):
                    for (MarkdownObject current = node, parent = node.Parent; current != null;)
                    {
                        if (parent is ContainerInline containerInline)
                        {
                            foreach (var child in containerInline)
                            {
                                if (child != current && child.IsVisible())
                                {
                                    return true;
                                }
                            }
                            current = parent;
                            parent = containerInline.Parent;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return node.GetPathToRootExclusive().Any(o => o is TableCell || o is RowBlock || o is NestedColumnBlock);
                default:
                    return false;
            }
        }

        private static bool IsInlineImage(this HtmlBlock node, SourceInfo<string> source)
        {
            var stack = new Stack<(HtmlToken? token, int elementCount, bool hasImg)>();
            stack.Push((null, 0, false));
            var reader = new HtmlReader(node.Lines.ToString());
            while (reader.Read(out var token))
            {
                var top = stack.Pop();
                switch (token.Type)
                {
                    case HtmlTokenType.StartTag:
                        if (token.IsInlineElement())
                        {
                            top.elementCount += 1;
                            if (token.NameIs("img"))
                            {
                                // Only look for the image specified by source info
                                var attributes = token.Attributes.ToArray().ToDictionary(a => a.Name.ToString(), StringComparer.InvariantCultureIgnoreCase);
                                if (attributes.TryGetValue("src", out var src) && SourceInfoMatch(source.Source!, src.ValueRange))
                                {
                                    top.hasImg = true;
                                }
                            }
                        }
                        else
                        {
                            if (top.hasImg && top.elementCount > 1)
                            {
                                return true;
                            }

                            top.elementCount = 0;
                            top.hasImg = false;
                        }
                        stack.Push(top);
                        if (!token.IsSelfClosing && !token.IsSelfClosingElement())
                        {
                            stack.Push((token, 0, false));
                        }
                        break;
                    case HtmlTokenType.EndTag:
                        if (!top.token.HasValue || !top.token.Value.NameIs(token.Name.Span))
                        {
                            // Invalid HTML structure, should throw warning
                            stack.Push(top);
                        }
                        else
                        {
                            if (top.hasImg)
                            {
                                if (top.elementCount > 1)
                                {
                                    return true;
                                }
                                if (top.token.Value.IsInlineElement())
                                {
                                    var parent = stack.Pop();
                                    parent.hasImg = true;
                                    stack.Push(parent);
                                }
                            }
                        }
                        break;
                    case HtmlTokenType.Text:
                        top.elementCount += token.RawText.Trim().Length > 0 ? 1 : 0;
                        stack.Push(top);
                        break;
                    default:
                        stack.Push(top);
                        break;
                }
            }

            // Should check if all tags are closed properly and throw warning if not
            return false;

            bool SourceInfoMatch(SourceInfo s, HtmlTextRange r)
            {
                return s.Line == (node.Line + r.Start.Line + 1) &&
                    s.EndLine == (node.Line + r.End.Line + 1) &&
                    s.Column == (r.Start.Column + 1) &&
                    s.EndColumn == (r.End.Column + 1);
            }
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
