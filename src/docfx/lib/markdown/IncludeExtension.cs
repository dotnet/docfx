// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class IncludeExtension
    {
        public static MarkdownPipelineBuilder UseExpandInclude(this MarkdownPipelineBuilder builder, MarkdownContext context, Func<List<Error>> getErrors)
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
            MarkdownContext context, MarkdownObject document, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, List<Error> errors)
        {
            document.Visit(obj =>
            {
                switch (obj)
                {
                    case InclusionBlock inclusionBlock:
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
            MarkdownContext context, InclusionBlock inclusionBlock, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, List<Error> errors)
        {
            var (content, file) = context.ReadFile(inclusionBlock.IncludedFilePath, inclusionBlock);
            if (content is null)
            {
                errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionBlock.IncludedFilePath, inclusionBlock.GetSourceInfo())));
                return;
            }

            if (InclusionContext.IsCircularReference(file, out var dependencyChain))
            {
                throw Errors.Link.CircularReference(new SourceInfo(((Document)file).FilePath), file, dependencyChain.Reverse()).ToException();
            }

            using (InclusionContext.PushInclusion(file))
            {
                var child = Markdown.Parse(content, pipeline);
                ExpandInclude(context, child, pipeline, inlinePipeline, errors);
                inclusionBlock.Add(child);
                inclusionBlock.ResolvedFilePath = file;
            }
        }

        private static void ExpandInclusionInline(
            MarkdownContext context, InclusionInline inclusionInline, MarkdownPipeline pipeline, MarkdownPipeline inlinePipeline, List<Error> errors)
        {
            var (content, file) = context.ReadFile(inclusionInline.IncludedFilePath, inclusionInline);
            if (content is null)
            {
                errors.Add(Errors.Markdown.IncludeNotFound(new SourceInfo<string?>(inclusionInline.IncludedFilePath, inclusionInline.GetSourceInfo())));
                return;
            }

            if (InclusionContext.IsCircularReference(file, out var dependencyChain))
            {
                throw Errors.Link.CircularReference(new SourceInfo(((Document)file).FilePath), file, dependencyChain.Reverse()).ToException();
            }

            using (InclusionContext.PushInclusion(file))
            {
                var child = Markdown.Parse(content, pipeline);
                ExpandInclude(context, child, pipeline, inlinePipeline, errors);

                foreach (var block in child)
                {
                    if (block is LeafBlock leaf && leaf.Inline != null)
                    {
                        inclusionInline.AppendChild(leaf.Inline);
                    }
                }

                inclusionInline.ResolvedFilePath = file;
            }
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
}
