// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Microsoft.DocAsCode.Common;

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
                .UseMarkdigAdvancedExtensions()
                .UseDfmExtensions(context)
                .RemoveUnusedExtensions();
        }

        public static MarkdownPipelineBuilder UseMarkdigAdvancedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            return pipeline
                //.UseMathematics()
                .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                .UseMediaLinks()
                .UsePipeTables()
                .UseAutoLinks();
        }

        public static MarkdownPipelineBuilder UseDfmExtensions(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            return pipeline
                .UseHeadingIdRewriter()
                .UseIncludeFile(context)
                .UseCodeSnippet(context)
                .UseYamlHeader()
                .UseDFMCodeInfoPrefix()
                .UseQuoteSectionNote(context)
                .UseXref()
                .UseEmojiAndSmiley(false)
                .UseTabGroup()
                .UseLineNumber(context)
                .UseMonikerRange()
                .UseValidators(context)
                .UseInteractiveCode()
                .UseRow()
                .UseNestedColumn()
                // Do not add extension after the InineParser
                .UseInlineParserOnly(context);
        }

        public static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseValidators(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            if (context.EnableValidation)
            {
                pipeline.Extensions.Add(new ValidationExtension(context));
            }
            return pipeline;
        }

        /// <summary>
        /// This extension removes all the block parser except paragragh. Please use this extension in the last.
        /// </summary>
        public static MarkdownPipelineBuilder UseInlineParserOnly(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            if (context.IsInline)
            {
                pipeline.Extensions.Add(new InlineOnlyExtentsion());
            }

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseTabGroup(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Add(new TabGroupExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseHeadingIdRewriter(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Add(new HeadingIdExtension());
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
                Logger.LogWarning($"Can't find FencedCodeBlockParser to set InfoPrefix, insert DFMFencedCodeBlockParser directly.");
                pipeline.BlockParsers.Insert(0, new FencedCodeBlockParser() { InfoPrefix = Constants.FencedCodePrefix });
            }
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseQuoteSectionNote(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new QuoteSectionNoteExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseLineNumber(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            if (!context.EnableSourceInfo)
            {
                return pipeline;
            }

            pipeline.Extensions.Add(new LineNumberExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseIncludeFile(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new InclusionExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseCodeSnippet(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new CodeSnippetExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseInteractiveCode(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Add(new InteractiveCodeExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseXref(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Insert(0, new XrefInlineExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseMonikerRange(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<MonikerRangeExtension>();
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseYamlHeader(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Insert(0, new YamlHeaderExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseRow(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<RowExtension>();
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseNestedColumn(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<NestedColumnExtension>();
            return pipeline;
        }
    }
}
