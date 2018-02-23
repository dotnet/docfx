// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using Markdig;
    using Markdig.Extensions.AutoIdentifiers;
    using Markdig.Extensions.CustomContainers;
    using Markdig.Parsers;
    using MarkdigEngine.Extensions;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using System.IO;

    public static class MarkdownExtensions
    {
        public static MarkdownPipelineBuilder UseMarkdigAdvancedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            return pipeline
                //.UseMathematics()
                //.UseEmphasisExtras()
                .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
                .UseMediaLinks()
                .UsePipeTables()
                .UseAutoLinks();
        }

        public static MarkdownPipelineBuilder UseDfmExtensions(
            this MarkdownPipelineBuilder pipeline,
            MarkdownEngine engine,
            MarkdownContext context,
            MarkdownServiceParameters parameters)
        {
            return pipeline
                .UseHeadingIdRewriter()
                .UseIncludeFile(engine, context, parameters)
                .UseCodeSnippet(engine, context)
                .UseYamlHeader()
                .UseDFMCodeInfoPrefix()
                .UseQuoteSectionNote(parameters)
                .UseXref()
                //.UseEmojiAndSmiley(false)
                .UseTabGroup()
                .UseInineParserOnly(context)
                .UseLineNumber(context, parameters)
                .UseMonikerRange()
                .UseValidators(context, parameters)
                .UseInteractiveCode()
                .UseRow()
                .UseNestedColumn();
        }

        public static MarkdownPipelineBuilder RemoveUnusedExtensions(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.RemoveAll(extension => extension is CustomContainerExtension);

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseValidators(this MarkdownPipelineBuilder pipeline, MarkdownContext context, MarkdownServiceParameters parameters)
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

        public static MarkdownPipelineBuilder UseQuoteSectionNote(this MarkdownPipelineBuilder pipeline, MarkdownServiceParameters parameters)
        {
            pipeline.Extensions.Insert(0, new QuoteSectionNoteExtension(parameters));
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseLineNumber(this MarkdownPipelineBuilder pipeline, MarkdownContext context, MarkdownServiceParameters parameters)
        {
            object enableSourceInfo = null;
            parameters?.Extensions?.TryGetValue(LineNumberExtension.EnableSourceInfo, out enableSourceInfo);

            var enabled = enableSourceInfo as bool?;
            if (enabled != null && enabled == false)
            {
                return pipeline;
            }

            var absoluteFilePath = Path.Combine(context.BasePath, context.FilePath);
            var lineNumberContext = LineNumberExtensionContext.Create(context.Content, absoluteFilePath, context.FilePath);

            pipeline.PreciseSourceLocation = true;
            pipeline.DocumentProcessed += LineNumberExtension.GetProcessDocumentDelegate(lineNumberContext);

            return pipeline;
        }

        public static MarkdownPipelineBuilder UseIncludeFile(this MarkdownPipelineBuilder pipeline, MarkdownEngine compositor, MarkdownContext context, MarkdownServiceParameters parameters)
        {
            pipeline.Extensions.Insert(0, new InclusionExtension(compositor, context, parameters));
            if (context.InclusionSet != null && !context.InclusionSet.IsEmpty)
            {
                pipeline.DocumentProcessed += InclusionExtension.GetProcessDocumentDelegate(context);
            }
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseCodeSnippet(this MarkdownPipelineBuilder pipeline, MarkdownEngine compositor, MarkdownContext context)
        {
            pipeline.Extensions.Insert(0, new CodeSnippetExtension(compositor, context));

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
            pipeline.Extensions.Insert(0, new RowExtension());
            return pipeline;
        }

        public static MarkdownPipelineBuilder UseNestedColumn(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.Insert(0, new NestedColumnExtension());
            return pipeline;
        }
    }
}
