// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Parsers;
using Markdig.Renderers;
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

        public static string ToPlainText(this MarkdownObject containerBlock)
        {
            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer)
            {
                EnableHtmlForBlock = false,
                EnableHtmlForInline = false,
                EnableHtmlEscape = false,
            };

            renderer.Render(containerBlock);
            writer.Flush();

            return writer.ToString();
        }

        public static bool IsVisible(this MarkdownObject markdownObject)
        {
            var visible = false;
            Visit(markdownObject, node =>
            {
                switch (node)
                {
                    case HtmlBlock htmlBlock:
                        foreach (var line in htmlBlock.Lines.Lines)
                        {
                            visible = visible || VisibleHtml(line.Slice.ToString());
                            if (visible)
                            {
                                return true;
                            }
                        }
                        return true;
                    case HtmlInline htmlInline:
                        visible = visible || VisibleHtml(htmlInline.Tag);
                        break;
                    case HeadingBlock headingBlock when headingBlock.Inline is null || !headingBlock.Inline.Any():
                        // empty heading
                    case ThematicBreakBlock _:
                        break;
                    case LeafBlock leafBlock when leafBlock.Inline is null || !leafBlock.Inline.Any():
                    case LeafInline _:
                        visible = true;
                        break;
                    default:
                        break;
                }

                return visible;
            });

            return visible;

            static bool VisibleHtml(string? html)
            {
                if (string.IsNullOrWhiteSpace(html))
                {
                    return false;
                }

                var visibleHtml = false;
                var reader = new HtmlReader(html);
                while (!visibleHtml && reader.Read(out var token))
                {
                    visibleHtml = visibleHtml || VisibleHtmlToken(token);
                }

                return visibleHtml;
            }

            static bool VisibleHtmlToken(HtmlToken token)
                => token.Type switch
                {
                    HtmlTokenType.Text => !token.RawText.Span.IsWhiteSpace(),
                    HtmlTokenType.Comment => false,
                    _ => true,
                };
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
