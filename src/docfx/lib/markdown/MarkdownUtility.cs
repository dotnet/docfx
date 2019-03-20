// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Threading;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    public enum MarkdownPipelineType
    {
        ConceptualMarkdown,
        InlineMarkdown,
        TocMarkdown,
        Markdown,
    }

    internal static class MarkdownUtility
    {
        private static readonly MarkdownPipeline[] s_markdownPipelines = new[]
        {
            CreateConceptualMarkdownPipeline(),
            CreateInlineMarkdownPipeline(),
            CreateTocMarkdownPipeline(),
            CreateMarkdownPipeline(),
        };

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

        public static MarkupResult Result => t_status.Value.Peek().Result;

        public static (MarkdownDocument ast, MarkupResult result) Parse(string content, MarkdownPipelineType piplineType)
        {
            try
            {
                var status = new Status { Result = new MarkupResult() };

                t_status.Value.Push(status);

                var ast = Markdown.Parse(content, s_markdownPipelines[(int)piplineType]);

                return (ast, Result);
            }
            finally
            {
                t_status.Value.Pop();
            }
        }

        public static (string html, MarkupResult result) ToHtml(
            string markdown,
            Document file,
            DependencyResolver dependencyResolver,
            Action<Document> buildChild,
            Func<string, List<string>> parseMonikerRange,
            Func<string, string> getToken,
            MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status
                    {
                        Result = new MarkupResult(),
                        DependencyResolver = dependencyResolver,
                        ParseMonikerRangeDelegate = parseMonikerRange,
                        GetToken = getToken,
                        BuildChild = buildChild,
                    };

                    t_status.Value.Push(status);

                    var html = Markdown.ToHtml(markdown, s_markdownPipelines[(int)pipelineType]);

                    return (html, Result);
                }
                finally
                {
                    t_status.Value.Pop();
                }
            }
        }

        private static MarkdownPipeline CreateConceptualMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(GetToken, LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .UseMonikerZone(ParseMonikerRange)
                .Build();
        }

        private static MarkdownPipeline CreateMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(GetToken, LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .Build();
        }

        private static MarkdownPipeline CreateInlineMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(GetToken, LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .UseInlineOnly()
                .Build();
        }

        private static MarkdownPipeline CreateTocMarkdownPipeline()
        {
            var builder = new MarkdownPipelineBuilder();

            // Only supports heading block and link inline
            builder.BlockParsers.RemoveAll(parser => !(
                parser is HeadingBlockParser || parser is ParagraphBlockParser ||
                parser is ThematicBreakParser || parser is HtmlBlockParser));

            builder.InlineParsers.RemoveAll(parser => !(parser is LinkInlineParser));

            builder.BlockParsers.Find<HeadingBlockParser>().MaxLeadingCount = int.MaxValue;

            builder.UseYamlFrontMatter()
                   .UseXref();

            return builder.Build();
        }

        private static string GetToken(string key)
        {
            return t_status.Value.Peek().GetToken(key);
        }

        private static void LogError(string code, string message, MarkdownObject origin, int? line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Error, code, message, InclusionContext.File.ToString(), origin.ToRange(line)));
        }

        private static void LogWarning(string code, string message, MarkdownObject origin, int? line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Warning, code, message, InclusionContext.File.ToString(), origin.ToRange(line)));
        }

        private static (string content, object file) ReadFile(string path, object relativeTo, MarkdownObject origin)
        {
            var (error, content, file) = t_status.Value.Peek().DependencyResolver.ResolveContent(path, (Document)relativeTo);
            Result.Errors.AddIfNotNull(error?.WithRange(origin.ToRange()));
            return (content, file);
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo, MarkdownObject origin)
        {
            var peek = t_status.Value.Peek();
            var (error, link, _) = peek.DependencyResolver.ResolveLink(path, (Document)relativeTo, (Document)resultRelativeTo, peek.BuildChild);
            Result.Errors.AddIfNotNull(error?.WithRange(origin.ToRange()));
            return link;
        }

        private static (Error error, string href, string display, Document file) ResolveXref(string href, MarkdownObject origin)
        {
            // TODO: now markdig engine combines all kinds of reference with inclusion, we need to split them out
            var result = t_status.Value.Peek().DependencyResolver.ResolveXref(href, (Document)InclusionContext.File, (Document)InclusionContext.RootFile);
            result.error = result.error?.WithRange(origin.ToRange());
            return result;
        }

        private static List<string> ParseMonikerRange(string monikerRange) => t_status.Value.Peek().ParseMonikerRangeDelegate(monikerRange);

        private sealed class Status
        {
            public MarkupResult Result;

            public DependencyResolver DependencyResolver;

            public Action<Document> BuildChild;

            public Func<string, List<string>> ParseMonikerRangeDelegate;

            public Func<string, string> GetToken;
        }
    }
}
