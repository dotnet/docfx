// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;

namespace Microsoft.Docs.Build
{
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

        private static readonly MarkdownPipeline s_markdownPipeline = CreateMarkdownPipeline();

        [ThreadStatic]
        private static (MarkupResult result, DependencyMapBuilder dependencyMap, Action<Document> buildChild) t_context;

        public static ref MarkupResult Result => ref t_context.result;

        public static (string html, MarkupResult result) ToHtml(
            string markdown, Document file, DependencyMapBuilder dependencyMap, Action<Document> buildChild)
        {
            using (InclusionContext.PushFile(file))
            {
                try
                {
                    t_context = (MarkupResult.Create(), dependencyMap, buildChild);
                    var html = Markdown.ToHtml(markdown, s_markdownPipeline);
                    return (html, t_context.result);
                }
                finally
                {
                    t_context = default;
                }
            }
        }

        private static MarkdownPipeline CreateMarkdownPipeline()
        {
            var markdownContext = new MarkdownContext(GetToken, LogWarning, LogError, ReadFile, GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseExtractYamlHeader()
                .UseExtractTitle()
                .UseResolveHtmlLinks(markdownContext)
                .UseResolveXref(ResolveXref)
                .Build();
        }

        private static string GetToken(string key)
        {
            return s_markdownTokens.TryGetValue(key, out var value) ? value : null;
        }

        private static void LogError(string code, string message, string doc, int line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Error, code, message, doc, line));
        }

        private static void LogWarning(string code, string message, string doc, int line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Warning, code, message, doc, line));
        }

        private static (string content, object file) ReadFile(string path, object relativeTo)
        {
            Debug.Assert(relativeTo is Document);

            var (error, content, child) = ((Document)relativeTo).TryResolveContent(path);

            if (error != null)
            {
                Result.Errors.Add(error);
            }

            t_context.dependencyMap.AddDependencyItem((Document)relativeTo, child, DependencyType.Inclusion);

            return (content, child);
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo)
        {
            Debug.Assert(relativeTo is Document);
            Debug.Assert(resultRelativeTo is Document);

            var (error, link, fragment, child) = ((Document)relativeTo).TryResolveHref(path, (Document)resultRelativeTo);

            if (error != null)
            {
                Result.Errors.Add(error);
            }

            if (child != null)
            {
                t_context.buildChild(child);
                t_context.dependencyMap.AddDependencyItem((Document)relativeTo, child, HrefUtility.FragmentToDependencyType(fragment));
            }

            return link;
        }

        private static string ResolveXref(string uid)
        {
            // TODO: implement xref resolve
            return null;
        }
    }
}
