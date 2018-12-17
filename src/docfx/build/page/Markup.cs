// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Resources;
using Markdig;
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

    internal static class Markup
    {
        private static readonly ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>> s_markdownTokens = new ConcurrentDictionary<string, Lazy<IReadOnlyDictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<MarkdownPipelineType, MarkdownPipeline> s_pipelineMapping =
            new Dictionary<MarkdownPipelineType, MarkdownPipeline>()
                {
                    { MarkdownPipelineType.ConceptualMarkdown, CreateConceptualMarkdownPipeline() },
                    { MarkdownPipelineType.InlineMarkdown, CreateInlineMarkdownPipeline() },
                    { MarkdownPipelineType.TocMarkdown, CreateTocPipeline() },
                    { MarkdownPipelineType.Markdown, CreateMarkdownPipeline() },
                };

        [ThreadStatic]
        private static ImmutableStack<Status> t_status;

        public static MarkupResult Result => t_status.Peek().Result;

        public static (MarkdownDocument ast, MarkupResult result) Parse(string content)
        {
            try
            {
                var status = new Status
                {
                    Result = new MarkupResult(),
                };
                t_status = t_status == null ? ImmutableStack.Create(status) : t_status.Push(status);
                var ast = Markdown.Parse(content, s_pipelineMapping[MarkdownPipelineType.TocMarkdown]);

                return (ast, Result);
            }
            finally
            {
                t_status = t_status.Pop();
            }
        }

        public static (string html, MarkupResult result) ToHtml(
            string markdown,
            Document file,
            DependencyResolver dependencyResolver,
            Action<Document> buildChild,
            Func<string, List<string>> parseMonikerRange,
            MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status
                    {
                        Result = new MarkupResult(),
                        Culture = file.Docset.Culture,
                        DependencyResolver = dependencyResolver,
                        ParseMonikerRangeDelegate = parseMonikerRange,
                        BuildChild = buildChild,
                    };
                    t_status = t_status is null ? ImmutableStack.Create(status) : t_status.Push(status);

                    var html = Markdown.ToHtml(markdown, s_pipelineMapping[pipelineType]);
                    if (pipelineType == MarkdownPipelineType.ConceptualMarkdown && !Result.HasTitle)
                    {
                        Result.Errors.Add(Errors.HeadingNotFound(file));
                    }
                    return (html, Result);
                }
                finally
                {
                    t_status = t_status.Pop();
                }
            }
        }

        private static MarkdownPipeline CreateConceptualMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(GetToken, LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseExtractTitle()
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

        private static MarkdownPipeline CreateTocPipeline()
        {
            var markdownContext = new MarkdownContext(null, LogWarning, LogError, null, null);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .Build();
        }

        private static string GetToken(string key)
        {
            var culture = t_status.Peek().Culture;
            var markdownTokens = s_markdownTokens.GetOrAdd(culture.ToString(), _ => new Lazy<IReadOnlyDictionary<string, string>>(() =>
            {
                var resourceManager = new ResourceManager("Microsoft.Docs.Template.resources.tokens", typeof(PageModel).Assembly);
                using (var resourceSet = resourceManager.GetResourceSet(culture, true, true))
                {
                    return resourceSet.Cast<DictionaryEntry>().ToDictionary(k => k.Key.ToString(), v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);
                }
            }));

            return markdownTokens.Value.TryGetValue(key, out var value) ? value : null;
        }

        private static void LogError(string code, string message, string doc, int line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Error, code, message, doc, new Range(line, 0)));
        }

        private static void LogWarning(string code, string message, string doc, int line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Warning, code, message, doc, new Range(line, 0)));
        }

        private static (string content, object file) ReadFile(string path, object relativeTo)
        {
            var (error, content, file) = t_status.Peek().DependencyResolver.ResolveContent(path, (Document)relativeTo);
            Result.Errors.AddIfNotNull(error);
            return (content, file);
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo)
        {
            var peek = t_status.Peek();
            var (error, link, _) = peek.DependencyResolver.ResolveLink(path, (Document)relativeTo, (Document)resultRelativeTo, peek.BuildChild);
            Result.Errors.AddIfNotNull(error);
            return link;
        }

        private static (Error error, string href, string display) ResolveXref(string href)
        {
            return t_status.Peek().DependencyResolver.ResolveXref(href, (Document)InclusionContext.File);
        }

        private static List<string> ParseMonikerRange(string monikerRange) => t_status.Peek().ParseMonikerRangeDelegate(monikerRange);

        private sealed class Status
        {
            public MarkupResult Result;

            public CultureInfo Culture;

            public DependencyResolver DependencyResolver;

            public Action<Document> BuildChild;

            public Func<string, List<string>> ParseMonikerRangeDelegate;
        }
    }
}
