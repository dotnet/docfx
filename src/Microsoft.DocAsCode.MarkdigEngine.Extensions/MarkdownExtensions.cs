// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Markdig;
    using Markdig.Extensions.AutoIdentifiers;
    using Markdig.Extensions.CustomContainers;
    using Markdig.Extensions.EmphasisExtras;
    using Markdig.Parsers;

    public static class MarkdownExtensions
    {
        public static MarkdownPipelineBuilder UseDocfxExtensions(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            return pipeline
                //.UseMathematics()
                .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                .UseMediaLinks()
                .UsePipeTables()
                .UseAutoLinks()
                .UseHeadingIdRewriter()
                .UseIncludeFile(context)
                .UseCodeSnippet(context)
                .UseDFMCodeInfoPrefix()
                .UseQuoteSectionNote(context)
                .UseXref()
                .UseEmojiAndSmiley(false)
                .UseTabGroup(context)
                .UseMonikerRange(context)
                .UseInteractiveCode()
                .UseRow(context)
                .UseNestedColumn(context)
                .UseTripleColon(context)
                .UseNoloc()
                .UseResolveLink(context)
                .RemoveUnusedExtensions();
        }

        /// <summary>
        /// Enables optional Markdig extensions that are not added by default with DocFX
        /// </summary>
        /// <param name="pipeline">The markdown pipeline builder</param>
        /// <param name="optionalExtensions">The list of optional extensions</param>
        /// <returns>The pipeline with optional extensions enabled</returns>
        public static MarkdownPipelineBuilder UseOptionalExtensions(
            this MarkdownPipelineBuilder pipeline,
            IEnumerable<string> optionalExtensions)
        {
            if (!optionalExtensions.Any())
            {
                return pipeline;
            }

            pipeline.Configure(string.Join("+", optionalExtensions));

            return pipeline;
        }

        private static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);
            return pipeline;
        }

        /// <summary>
        /// This extension removes all the block parser except paragraph. Please use this extension in the last.
        /// </summary>
        public static MarkdownPipelineBuilder UseInlineOnly(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready(new InlineOnlyExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseTabGroup(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new TabGroupExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseHeadingIdRewriter(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready(new HeadingIdExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseDFMCodeInfoPrefix(this MarkdownPipelineBuilder pipeline)
        {
            var fencedCodeBlockParser = pipeline.BlockParsers.FindExact<FencedCodeBlockParser>();
            if (fencedCodeBlockParser != null)
            {
                fencedCodeBlockParser.InfoPrefix = Constants.FencedCodePrefix;
            }
            else
            {
                pipeline.BlockParsers.AddIfNotAlready(new FencedCodeBlockParser { InfoPrefix = Constants.FencedCodePrefix });
            }
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseQuoteSectionNote(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new QuoteSectionNoteExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseLineNumber(this MarkdownPipelineBuilder pipeline, Func<object, string> getFilePath = null)
        {
            pipeline.Extensions.AddIfNotAlready(new LineNumberExtension(getFilePath));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseResolveLink(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new ResolveLinkExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseIncludeFile(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new InclusionExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseCodeSnippet(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new CodeSnippetExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseInteractiveCode(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready(new InteractiveCodeExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseXref(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready(new XrefInlineExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseMonikerRange(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new MonikerRangeExtension(context));
            return pipeline;
        }
        public static MarkdownPipelineBuilder UseRow(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new RowExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseNestedColumn(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new NestedColumnExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseTripleColon(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.AddIfNotAlready(new TripleColonExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseNoloc(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready(new NolocExtension());
            return pipeline;
        }
    }
}
