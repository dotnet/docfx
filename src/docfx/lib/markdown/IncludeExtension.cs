// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Docs.MarkdigExtensions;

namespace Microsoft.Docs.Build;

internal static class IncludeExtension
{
    public static MarkdownPipelineBuilder UseExpandInclude(this MarkdownPipelineBuilder builder, MarkdownContext context, Func<ErrorBuilder> getErrors)
    {
        var pipeline = CreateMarkdownPipeline(builder);
        var inlinePipeline = CreateMarkdownPipeline(builder, inlineOnly: true);

        return builder
            .Use(RemoveIncludeRenderer)
            .Use(ExpandInclude);

        void ExpandInclude(MarkdownDocument document)
        {
            var errors = getErrors();
            IncludeExtension.ExpandInclude(context, document, pipeline, inlinePipeline, errors);
        }
    }

    private static void RemoveIncludeRenderer(IMarkdownRenderer renderer)
    {
        renderer.ObjectRenderers.RemoveAll(r => r is HtmlInclusionBlockRenderer);
        renderer.ObjectRenderers.RemoveAll(r => r is HtmlInclusionInlineRenderer);
    }

    private static void ExpandInclude(
        MarkdownContext context, MarkdownObject document, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, ErrorBuilder errors)
    {
        document.Visit(obj =>
        {
            switch (obj)
            {
                case InclusionBlock inclusionBlock:
                    ExpandInclusionBlock(context, inclusionBlock, pipeline, inlinePipeline, errors);
                    return true;

                case InclusionInline inclusionInline when TryConvertToInclusionBlock(inclusionInline) is InclusionBlock inclusionBlock:
                    ExpandInclusionBlock(context, inclusionBlock, pipeline, inlinePipeline, errors);
                    return true;

                case InclusionInline inclusionInline:
                    ExpandInclusionInline(context, inclusionInline, inlinePipeline, inlinePipeline, errors);
                    return true;

                default:
                    return false;
            }
        });
    }

    private static void ExpandInclusionBlock(
        MarkdownContext context, InclusionBlock inclusionBlock, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, ErrorBuilder errors)
    {
        if (string.IsNullOrEmpty(inclusionBlock.IncludedFilePath))
        {
            errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionBlock.IncludedFilePath, inclusionBlock.GetSourceInfo())));
            return;
        }
        if (!inclusionBlock.IncludedFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Errors.Markdown.IncludeInvalid(new SourceInfo<string?>(inclusionBlock.IncludedFilePath, inclusionBlock.GetSourceInfo())));
            return;
        }

        var (content, file) = context.ReadFile(inclusionBlock.IncludedFilePath, inclusionBlock);
        if (content is null || file is null)
        {
            errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionBlock.IncludedFilePath, inclusionBlock.GetSourceInfo())));
            return;
        }

        if (InclusionContext.IsCircularReference(file, out var dependencyChain))
        {
            throw Errors.Link.CircularReference((SourceInfo)file, file, dependencyChain.Reverse()).ToException();
        }

        using (InclusionContext.PushInclusion(file))
        {
            var child = Markdown.Parse(content, pipeline);
            child.SetSourceInfo((SourceInfo)file);
            ExpandInclude(context, child, pipeline, inlinePipeline, errors);
            inclusionBlock.Add(child);
        }
    }

    private static void ExpandInclusionInline(
        MarkdownContext context, InclusionInline inclusionInline, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, ErrorBuilder errors)
    {
        if (string.IsNullOrEmpty(inclusionInline.IncludedFilePath))
        {
            errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionInline.IncludedFilePath, inclusionInline.GetSourceInfo())));
            return;
        }
        if (!inclusionInline.IncludedFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Errors.Markdown.IncludeInvalid(new SourceInfo<string?>(inclusionInline.IncludedFilePath, inclusionInline.GetSourceInfo())));
            return;
        }

        var (content, file) = context.ReadFile(inclusionInline.IncludedFilePath, inclusionInline);
        if (content is null)
        {
            errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionInline.IncludedFilePath, inclusionInline.GetSourceInfo())));
            return;
        }

        if (InclusionContext.IsCircularReference(file, out var dependencyChain))
        {
            throw Errors.Link.CircularReference((SourceInfo)file, file, dependencyChain.Reverse()).ToException();
        }

        using (InclusionContext.PushInclusion(file))
        {
            var child = Markdown.Parse(content, pipeline);
            ExpandInclude(context, child, pipeline, inlinePipeline, errors);

            foreach (var block in child)
            {
                if (block is LeafBlock leaf && leaf.Inline != null)
                {
                    leaf.Inline.SetSourceInfo((SourceInfo)file);
                    inclusionInline.AppendChild(leaf.Inline);
                }
            }
        }
    }

    private static InclusionBlock? TryConvertToInclusionBlock(InclusionInline inline)
    {
        // If a table cell contains only an InclusionInline token,
        // treat that InclusionInline as InclusionBlock to allow list inside table.
        if (inline.Parent is ContainerInline containerInline &&
            containerInline.ParentBlock is ParagraphBlock paragraphBlock &&
            paragraphBlock.Parent is TableCell tableCell &&
            tableCell.Count == 1 &&
            containerInline.All(child => child == inline || IsEmptyLiteralInline(child)))
        {
            var inclusionBlock = new InclusionBlock(null) { Title = inline.Title, IncludedFilePath = inline.IncludedFilePath };
            tableCell.RemoveAt(0);
            tableCell.Add(inclusionBlock);
            return inclusionBlock;
        }

        return null;

        static bool IsEmptyLiteralInline(Inline inline) => inline is LiteralInline literal && literal.Content.IsEmptyOrWhitespace();
    }

    private static MarkdownPipeline CreateMarkdownPipeline(MarkdownPipelineBuilder existingBuilder, bool inlineOnly = false)
    {
        var builder = new MarkdownPipelineBuilder();

        foreach (var extension in existingBuilder.Extensions)
        {
            builder.Extensions.Add(extension);
        }

        if (inlineOnly)
        {
            builder.UseInlineOnly();
        }

        return builder.Build();
    }
}
