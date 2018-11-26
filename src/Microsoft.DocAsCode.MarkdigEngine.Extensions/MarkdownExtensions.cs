// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
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
                .RemoveUnusedExtensions();
        }

        private static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);
            return pipeline;
        }
    
        /// <summary>	
        /// This extension removes all the block parser except paragragh. Please use this extension in the last.	
        /// </summary>
        public static MarkdownPipelineBuilder UseInlineOnly(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Add(new InlineOnlyExtentsion());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseTabGroup(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Add(new TabGroupExtension(context));
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
                pipeline.BlockParsers.Insert(0, new FencedCodeBlockParser() { InfoPrefix = Constants.FencedCodePrefix });
            }
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseQuoteSectionNote(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new QuoteSectionNoteExtension(context));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseLineNumber(this MarkdownPipelineBuilder pipeline, Func<object, string> getFilePath = null)
        {
            pipeline.Extensions.Add(new LineNumberExtension(getFilePath));
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
            pipeline.Extensions.Insert(0, new TripleColonExtension(context));
            return pipeline;
        }
    }
}
