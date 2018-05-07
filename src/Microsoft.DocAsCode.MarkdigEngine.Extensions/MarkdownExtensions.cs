// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System.Linq;

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
                .UseInineParserOnly(context);
        }

        public static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseValidators(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            var tokenRewriter = context.Mvb.CreateRewriter();
            var visitor = new MarkdownDocumentVisitor(tokenRewriter);

            pipeline.DocumentProcessed += document =>
            {
                visitor.Visit(document);
            };

            return pipeline;
        }

        /// <summary>
        /// This extension removes all the block parser except paragragh. Please use this extension in the last.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static MarkdownPipelineBuilder UseInineParserOnly(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            if (context.IsInline)
            {
                pipeline.Extensions.Add(new InlineOnlyExtentsion());
            }

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseTabGroup(this MarkdownPipelineBuilder pipeline)
        {
            var tabGroupAggregator = new TabGroupAggregator();
            var aggregateVisitor = new MarkdownDocumentAggregatorVisitor(tabGroupAggregator);

            var tagGroupIdRewriter = new TabGroupIdRewriter();
            var tagGroupIdVisitor = new MarkdownDocumentVisitor(tagGroupIdRewriter);

            var activeAndVisibleRewriter = new ActiveAndVisibleRewriter();
            var activeAndVisibleVisitor = new MarkdownDocumentVisitor(activeAndVisibleRewriter);

            pipeline.DocumentProcessed += document =>
            {
                aggregateVisitor.Visit(document);
                tagGroupIdVisitor.Visit(document);
                activeAndVisibleVisitor.Visit(document);
            };

            pipeline.Extensions.Add(new TabGroupExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseHeadingIdRewriter(this MarkdownPipelineBuilder pipeline)
        {
            var tokenRewriter = new HeadingIdRewriter();
            var visitor = new MarkdownDocumentVisitor(tokenRewriter);

            pipeline.DocumentProcessed += document =>
            {
                visitor.Visit(document);
            };

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

            pipeline.PreciseSourceLocation = true;
            pipeline.DocumentProcessed += LineNumberExtension.GetProcessDocumentDelegate(context.GetFilePath(context.File));

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseIncludeFile(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new InclusionExtension(context));
            pipeline.DocumentProcessed += InclusionExtension.GetProcessDocumentDelegate(context);
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseCodeSnippet(this MarkdownPipelineBuilder pipeline, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new CodeSnippetExtension(context));

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseInteractiveCode(this MarkdownPipelineBuilder pipeline)
        {
            var codeSnippetInteractiveRewriter = new CodeSnippetInteractiveRewriter();
            var fencedCodeInteractiveRewrtier = new FencedCodeInteractiveRewriter();

            var codeSnippetVisitor = new MarkdownDocumentVisitor(codeSnippetInteractiveRewriter);
            var fencedCodeVisitor = new MarkdownDocumentVisitor(fencedCodeInteractiveRewrtier);

            pipeline.DocumentProcessed += document =>
            {
                codeSnippetVisitor.Visit(document);
                fencedCodeVisitor.Visit(document);
            };

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
