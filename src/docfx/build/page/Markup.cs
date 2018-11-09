// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

        private static readonly ConcurrentDictionary<MarkdownPipelineType, Lazy<MarkdownPipeline>> s_pipelineMapping = new ConcurrentDictionary<MarkdownPipelineType, Lazy<MarkdownPipeline>>();

        [ThreadStatic]
        private static ImmutableStack<Status> t_status;

        public static MarkupResult Result => t_status.Peek().Result;

        public static (MarkdownDocument ast, MarkupResult result) Parse(string content, string locale)
        {
            try
            {
                var status = new Status
                {
                    Result = new MarkupResult(),
                };
                t_status = t_status == null ? ImmutableStack.Create(status) : t_status.Push(status);
                var ast = Markdown.Parse(content, GetPipeline(MarkdownPipelineType.TocMarkdown, locale));

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
            Func<string, object, (string, object)> readFile,
            Func<string, object, object, string> getLink,
            Func<string, XrefSpec> resolveXref,
            MarkdownPipelineType pipelineType)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    var status = new Status
                    {
                        Result = new MarkupResult(),
                        ReadFileDelegate = readFile,
                        GetLinkDelegate = getLink,
                        ResolveXrefDelegate = resolveXref,
                    };
                    t_status = t_status is null ? ImmutableStack.Create(status) : t_status.Push(status);

                    var html = Markdown.ToHtml(markdown, GetPipeline(pipelineType, file.Docset.Locale));
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

        private static MarkdownPipeline GetPipeline(MarkdownPipelineType pipelineType, string locale)
        {
            return s_pipelineMapping.GetOrAdd(pipelineType, key => new Lazy<MarkdownPipeline>(() =>
            {
                switch (key)
                {
                    case MarkdownPipelineType.ConceptualMarkdown:
                        return CreateConceptualMarkdownPipeline(locale);
                    case MarkdownPipelineType.InlineMarkdown:
                        return CreateInlineMarkdownPipeline(locale);
                    case MarkdownPipelineType.Markdown:
                        return CreateMarkdownPipeline(locale);
                    case MarkdownPipelineType.TocMarkdown:
                        return CreateTocPipeline();
                    default:
                        throw new NotSupportedException($"{pipelineType} is not supported yet");
                }
            })).Value;
        }

        private static MarkdownPipeline CreateConceptualMarkdownPipeline(string locale)
        {
            var markdownContext = new MarkdownContext(key => GetToken(key, locale), LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseExtractTitle()
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .Build();
        }

        private static MarkdownPipeline CreateMarkdownPipeline(string locale)
        {
            var markdownContext = new MarkdownContext(key => GetToken(key, locale), LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .Build();
        }

        private static MarkdownPipeline CreateInlineMarkdownPipeline(string locale)
        {
            var markdownContext = new MarkdownContext(key => GetToken(key, locale), LogWarning, LogError, ReadFile, GetLink);

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

        private static string GetToken(string key, string locale)
        {
            var markdownTokens = s_markdownTokens.GetOrAdd(locale, _ => new Lazy<IReadOnlyDictionary<string, string>>(() =>
            {
                var tokenFile = $"resources/tokens.{locale}.json";
                var tokens = (Dictionary<string, string>)null;
                if (File.Exists(tokenFile))
                {
                    tokens = JsonUtility.Deserialize<Dictionary<string, string>>(File.ReadAllText(tokenFile)).model;
                }

                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (tokens != null)
                {
                    foreach (var token in tokens)
                    {
                        result[token.Key] = token.Value;
                    }
                }
                return result;
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

        private static (string content, object file) ReadFile(string path, object relativeTo) => t_status.Peek().ReadFileDelegate(path, relativeTo);

        private static string GetLink(string path, object relativeTo, object resultRelativeTo) => t_status.Peek().GetLinkDelegate(path, relativeTo, resultRelativeTo);

        private static XrefSpec ResolveXref(string uid) => t_status.Peek().ResolveXrefDelegate(uid);

        private sealed class Status
        {
            public MarkupResult Result { get; set; }

            public Func<string, object, (string, object)> ReadFileDelegate { get; set; }

            public Func<string, object, object, string> GetLinkDelegate { get; set; }

            public Func<string, XrefSpec> ResolveXrefDelegate { get; set; }
        }
    }
}
