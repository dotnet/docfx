// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using Markdig;
using Markdig.Parsers;
using Markdig.Parsers.Inlines;
using Markdig.Syntax;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal class MarkdownEngine
    {
        private readonly LinkResolver _linkResolver;
        private readonly XrefResolver _xrefResolver;
        private readonly MonikerProvider _monikerProvider;
        private readonly TemplateEngine _templateEngine;
        private readonly string _markdownValidationRules;

        private readonly MarkdownContext _markdownContext;
        private readonly MarkdownPipeline[] _pipelines;

        private static readonly ThreadLocal<Stack<Status>> t_status = new ThreadLocal<Stack<Status>>(() => new Stack<Status>());

        public MarkdownEngine(
            Config config,
            FileResolver fileResolver,
            LinkResolver linkResolver,
            XrefResolver xrefResolver,
            MonikerProvider monikerProvider,
            TemplateEngine templateEngine)
        {
            _linkResolver = linkResolver;
            _xrefResolver = xrefResolver;
            _monikerProvider = monikerProvider;
            _templateEngine = templateEngine;

            _markdownContext = new MarkdownContext(GetToken, LogInfo, LogSuggestion, LogWarning, LogError, ReadFile);
            _markdownValidationRules = config.MarkdownValidationRules;
            if (!string.IsNullOrEmpty(_markdownValidationRules))
            {
                using var stream = fileResolver.ReadStream(config.MarkdownValidationRules);

                // TODO: validation rules currently only supports physical file.
                _markdownValidationRules = ((FileStream)stream).Name;
            }

            _pipelines = new[]
            {
                CreateMarkdownPipeline(),
                CreateInlineMarkdownPipeline(),
                CreateTocMarkdownPipeline(),
            };
        }

        public (List<Error> errors, MarkdownDocument ast) Parse(string content, MarkdownPipelineType piplineType)
        {
            try
            {
                var status = new Status();

                t_status.Value!.Push(status);

                var ast = Markdown.Parse(content, _pipelines[(int)piplineType]);

                return (status.Errors, ast);
            }
            finally
            {
                t_status.Value!.Pop();
            }
        }

        public (List<Error> errors, string html) ToHtml(string markdown, Document file, MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status();

                    t_status.Value!.Push(status);

                    var html = Markdown.ToHtml(markdown, _pipelines[(int)pipelineType]);

                    return (status.Errors, html);
                }
                finally
                {
                    t_status.Value!.Pop();
                }
            }
        }

        private MarkdownPipeline CreateMarkdownPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(_markdownContext)
                .UseLink(GetLink)
                .UseXref(GetXref)
                .UseMonikerZone(GetMonikerRange)
                .UseContentValidation(_markdownContext, _markdownValidationRules)
                .Build();
        }

        private MarkdownPipeline CreateInlineMarkdownPipeline()
        {
            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(_markdownContext)
                .UseLink(GetLink)
                .UseXref(GetXref)
                .UseMonikerZone(GetMonikerRange)
                .UseContentValidation(_markdownContext, _markdownValidationRules)
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

        private string? GetToken(string key)
        {
            return _templateEngine.GetToken(key);
        }

        private static void LogInfo(string code, string message, MarkdownObject origin, int? line)
        {
            Log.Write($"{code}: {message}");
        }

        private static void LogError(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value!.Peek().Errors.Add(new Error(ErrorLevel.Error, code, message, origin.ToSourceInfo(line)));
        }

        private static void LogWarning(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value!.Peek().Errors.Add(new Error(ErrorLevel.Warning, code, message, origin.ToSourceInfo(line)));
        }

        private static void LogSuggestion(string code, string message, MarkdownObject origin, int? line)
        {
            t_status.Value!.Peek().Errors.Add(new Error(ErrorLevel.Suggestion, code, message, origin.ToSourceInfo(line)));
        }

        private (string content, object? file) ReadFile(string path, object relativeTo, MarkdownObject origin)
        {
            var status = t_status.Value!.Peek();
            var (error, content, file) = _linkResolver.ResolveContent(new SourceInfo<string>(path, origin.ToSourceInfo()), (Document)relativeTo);
            status.Errors.AddIfNotNull(error);
            return (content, file);
        }

        private string GetLink(SourceInfo<string> href)
        {
            var status = t_status.Value!.Peek();
            var (error, link, file) = _linkResolver.ResolveLink(
                href, (Document)InclusionContext.File, (Document)InclusionContext.RootFile);

            status.Errors.AddIfNotNull(error);
            return link;
        }

        private (string href, string display) GetXref(SourceInfo<string> href, bool isShorthand)
        {
            var status = t_status.Value!.Peek();
            var (error, link, display, _) = _xrefResolver.ResolveXref(
                href, (Document)InclusionContext.File, (Document)InclusionContext.RootFile);

            if (!isShorthand)
            {
                status.Errors.AddIfNotNull(error);
            }
            return (link, display);
        }

        private IReadOnlyList<string> GetMonikerRange(SourceInfo<string> monikerRange)
        {
            var status = t_status.Value!.Peek();
            var (error, monikers) = _monikerProvider.GetZoneLevelMonikers(((Document)InclusionContext.RootFile).FilePath, monikerRange);
            status.Errors.AddIfNotNull(error);
            return monikers;
        }

        private class Status
        {
            public List<Error> Errors { get; } = new List<Error>();
        }
    }
}
