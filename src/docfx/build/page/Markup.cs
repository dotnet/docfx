// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        // In docfx 2, a localized text is prepended to quotes beginning with
        // [!NOTE], [!TIP], [!WARNING], [!IMPORTANT], [!CAUTION].
        //
        // Docfx 2 reads localized tokens from template repo. In docfx3, build (excluding static page generation)
        // does not depend on template, thus these tokens are managed by us.
        //
        // TODO: add localized tokens
        private static readonly IReadOnlyDictionary<string, string> s_markdownTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Note", "<p>Note</p>" },
            { "Tip", "<p>Tip</p>" },
            { "Warning", "<p>Warning</p>" },
            { "Important", "<p>Important</p>" },
            { "Caution", "<p>Caution</p>" },
        };

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
            return s_markdownTokens.TryGetValue(key, out var value) ? value : null;
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
