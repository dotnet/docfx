// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using HtmlAgilityPack;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
    internal static class MarkdownUtility
    {
        // This magic string identifies if an URL was a relative URL in source,
        // URLs starting with this magic string are transformed into relative URL after markup.
        private const string RelativeUrlMarker = "//////";

        private static readonly MarkdownPipeline[] s_markdownPipelines = new[]
        {
            CreateMarkdownPipeline(),
            CreateInlineMarkdownPipeline(),
            CreateTocMarkdownPipeline(),
        };

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

        public static (List<Error> errors, MarkdownDocument ast) Parse(string content, MarkdownPipelineType piplineType)
        {
            try
            {
                var status = new Status { Errors = new List<Error>() };

                t_status.Value.Push(status);

                var ast = Markdown.Parse(content, s_markdownPipelines[(int)piplineType]);

                return (status.Errors, ast);
            }
            finally
            {
                t_status.Value.Pop();
            }
        }

        public static (List<Error> errors, HtmlNode html) ToHtml(
            Context context,
            string markdown,
            Document file,
            MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status
                    {
                        Errors = new List<Error>(),
                        Context = context,
                    };

                    t_status.Value.Push(status);

                    var html = Markdown.ToHtml(markdown, s_markdownPipelines[(int)pipelineType]);
                    var htmlNode = HtmlUtility.LoadHtml(html);

                    var htmlNodeWithRelativeLink = HtmlUtility.TransformLinks(htmlNode, (href, _) =>
                    {
                        if (href.StartsWith(RelativeUrlMarker))
                        {
                            return UrlUtility.GetRelativeUrl(file.SiteUrl, href.Substring(RelativeUrlMarker.Length));
                        }
                        return href;
                    });

                    return (status.Errors, htmlNodeWithRelativeLink.StripTags().RemoveRerunCodepenIframes());
                }
                finally
                {
                    t_status.Value.Pop();
                }
            }
        }

        private static MarkdownPipeline CreateMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(
                GetToken,
                (code, message, origin, line) => Log.Write(message),
                LogSuggestion,
                LogWarning,
                LogError,
                ReadFile);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseLink(GetLink)
                .UseXref(GetXref)
                .UseMonikerZone(GetMonikerRange)
                .Build();
        }

        private static MarkdownPipeline CreateInlineMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(
                GetToken,
                (code, message, origin, line) => Log.Write(message),
                LogSuggestion,
                LogWarning,
                LogError,
                ReadFile);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseLink(GetLink)
                .UseXref(GetXref)
                .UseMonikerZone(GetMonikerRange)
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
                   .UseXref()
                   .UsePreciseSourceLocation();

            return builder.Build();
        }

        private static string GetToken(string key)
        {
            return t_status.Value.Peek().Context.TemplateEngine.GetToken(key);
        }

        private static void LogError(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value.Peek().Errors.Add(new Error(ErrorLevel.Error, code, message, origin.ToSourceInfo(line)));
        }

        private static void LogWarning(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value.Peek().Errors.Add(new Error(ErrorLevel.Warning, code, message, origin.ToSourceInfo(line)));
        }

        private static void LogSuggestion(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value.Peek().Errors.Add(new Error(ErrorLevel.Suggestion, code, message, origin.ToSourceInfo(line)));
        }

        private static (string content, object file) ReadFile(string path, object relativeTo, MarkdownObject origin)
        {
            var status = t_status.Value.Peek();
            var (error, content, file) = status.Context.DependencyResolver.ResolveContent(new SourceInfo<string>(path, origin.ToSourceInfo()), (Document)relativeTo);
            status.Errors.AddIfNotNull(error);
            return (content, file);
        }

        private static string GetLink(SourceInfo<string> href)
        {
            var status = t_status.Value.Peek();
            var (error, link, file) = status.Context.DependencyResolver.ResolveAbsoluteLink(
                href, (Document)InclusionContext.File);

            if (file != null)
            {
                link = RelativeUrlMarker + link;
            }

            status.Errors.AddIfNotNull(error);
            return link;
        }

        private static (string href, string display) GetXref(SourceInfo<string> href, bool isShorthand)
        {
            var status = t_status.Value.Peek();
            var (error, link, display, spec) = status.Context.DependencyResolver.ResolveAbsoluteXref(
                href, (Document)InclusionContext.File);

            if (spec?.DeclaringFile != null)
            {
                link = RelativeUrlMarker + link;
            }

            if (!isShorthand)
            {
                status.Errors.AddIfNotNull(error);
            }
            return (link, display);
        }

        private static List<string> GetMonikerRange(SourceInfo<string> monikerRange)
        {
            var status = t_status.Value.Peek();
            var (error, monikers) = status.Context.MonikerProvider.GetZoneLevelMonikers((Document)InclusionContext.File, monikerRange);
            status.Errors.AddIfNotNull(error);
            return monikers;
        }

        private sealed class Status
        {
            public List<Error> Errors;

            public Context Context;
        }
    }
}
