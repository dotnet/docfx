// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using System;
    using System.Collections.Generic;
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
        /// <param name="context">The markdown context</param>
        /// <param name="extensions">The extension dictionary</param>
        /// <returns>The pipeline with optional extensions enabled</returns>
        public static MarkdownPipelineBuilder UseOptionalExtensions(
            this MarkdownPipelineBuilder pipeline,
            MarkdownContext context,
            IReadOnlyDictionary<string, object> extensions)
        {
            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableTaskLists, extensions))
            {
                pipeline.UseTaskLists();
            }

            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableGridTables, extensions))
            {
                pipeline.UseGridTables();
            }

            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableFootnotes, extensions))
            {
                pipeline.UseFootnotes();
            }

            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableMathematics, extensions))
            {
                pipeline.UseMathematics();
            }

            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableDiagrams, extensions))
            {
                pipeline.UseDiagrams();
            }

            if (IsExtensionEnabled(Constants.OptionalExtensionPropertyNames.EnableDefinitionLists, extensions))
            {
                pipeline.UseDefinitionLists();
            }

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

        /// <summary>
        /// Checks the extensions dictionary if an extension is enabled.
        /// </summary>
        /// <param name="extensionPropertyName">The property name for the extension</param>
        /// <param name="extensions">The read-only dictionary containing the extensions</param>
        /// <returns>True if the extension is in the dictionary and its value is set to true. False, otherwise.</returns>
        private static bool IsExtensionEnabled(string extensionPropertyName, IReadOnlyDictionary<string, object> extensions)
        {
            object enableExtensionObj = null;
            extensions?.TryGetValue(extensionPropertyName, out enableExtensionObj);

            return enableExtensionObj is bool enabled && enabled;
        }
    }
}
