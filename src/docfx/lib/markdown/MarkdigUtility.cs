// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

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

                return properties.FirstOrDefault(p => p.Key == "data-target").Value;
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

    public static IReadOnlyCollection<string>? GetZonePivots(this MarkdownObject obj)
    {
        foreach (var item in obj.GetPathToRootInclusive())
        {
            if (item is TripleColonBlock block && block.Extension is ZoneExtension zone && block.Attributes.TryGetValue("pivot", out var pivot) &&
                (!block.Attributes.TryGetValue("target", out var target) || string.Equals(target, "docs", StringComparison.OrdinalIgnoreCase)))
            {
                return pivot.Split(",").Select(x => x.Trim()).ToList();
            }
        }

        return default;
    }

    public static string? GetTabId(this MarkdownObject obj)
    {
        foreach (var parent in obj.GetPathToRootInclusive())
        {
            if (parent is TabContentBlock content)
            {
                return content.Id;
            }
        }
        return null;
    }

    public static IEnumerable<MarkdownObject> GetPathToRootInclusive(this MarkdownObject obj)
    {
        yield return obj;

        foreach (var item in obj.GetPathToRootExclusive())
        {
            yield return item;
        }
    }

    public static IEnumerable<MarkdownObject> GetPathToRootExclusive(this MarkdownObject obj)
    {
        while (true)
        {
            var parent = obj switch
            {
                Block block => block.Parent,
                ContainerInline containerInline => containerInline.Parent as MarkdownObject ?? containerInline.ParentBlock,
                Inline inline => inline.Parent,
                _ => null,
            };

            if (parent is null)
            {
                if (obj is MarkdownDocument)
                {
                    yield break;
                }

                throw new InvalidOperationException(
                    "This operation is not supported in a markdig parser extension, move it to the render extension or the DocumentProcessed handler.");
            }

            obj = parent;

            yield return obj;
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
                        if (ReplaceCore(child, action) is not Inline replacement)
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
                    var leafInline = ReplaceCore(leaf.Inline, action) as ContainerInline;
                    if (leafInline != leaf.Inline)
                    {
                        leaf.Inline = leafInline;
                    }
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
                LinkReferenceDefinition => false,
                ThematicBreakBlock => false,
                YamlFrontMatterBlock => false,
                HeadingBlock headingBlock when headingBlock.Inline is null || !headingBlock.Inline.Any() => false,
                CodeBlock codeBlock when codeBlock.Lines.Count != 0 => true,
                LeafBlock leafBlock when leafBlock.Inline is null || !leafBlock.Inline.Any() => false,
                LinkInline linkInline when linkInline.IsImage => true,
                TripleColonInline tripleColonInline when tripleColonInline.Extension is ImageExtension => true,
                TripleColonBlock tripleColonBlock when tripleColonBlock.Extension is ImageExtension => true,
                QuoteSectionNoteBlock quoteSectionNoteBlock when quoteSectionNoteBlock.QuoteType is QuoteSectionNoteType.DFMVideo => true,
                LiteralInline literal when literal.Content.IsEmptyOrWhitespace() => false,
                LeafInline => true,
                _ => false,
            };
            return visible = nodeVisible || visible;
        });

        return visible;
    }

    public static bool IsInlineImage(this MarkdownObject node, int imageIndex)
    {
        return node switch
        {
            Inline inline => inline.IsInlineImage(),
            HtmlBlock htmlBlock => htmlBlock.IsInlineImage(imageIndex),
            _ => false,
        };
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
                            if (child.IsVisible())
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
