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
        private static MarkupResult t_result;

        [ThreadStatic]
        private static DependencyMapBuilder t_dependencyMap;

        [ThreadStatic]
        private static BookmarkValidator t_bookmarkValidator;

        [ThreadStatic]
        private static Action<Document> t_buildChild;

        public static MarkupResult Result => t_result;

        public static (string html, MarkupResult result) ToHtml(
            string markdown,
            Document file,
            DependencyMapBuilder dependencyMap,
            BookmarkValidator bookmarkValidator,
            Action<Document> buildChild)
        {
            if (t_result != null)
            {
                throw new NotImplementedException("Nested call to Markup.ToHtml");
            }

            using (InclusionContext.PushFile(file))
            {
                try
                {
                    t_result = new MarkupResult();
                    t_dependencyMap = dependencyMap;
                    t_bookmarkValidator = bookmarkValidator;
                    t_buildChild = buildChild;
                    var html = Markdown.ToHtml(markdown, s_markdownPipeline);
                    if (!t_result.HasTitle)
                    {
                        t_result.Errors.Add(Errors.HeadingNotFound(file));
                    }
                    return (html, t_result);
                }
                finally
                {
                    t_result = null;
                    t_dependencyMap = null;
                    t_bookmarkValidator = null;
                    t_buildChild = null;
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
            Result.Errors.Add(new Error(ErrorLevel.Error, code, message, doc, new Range(line, 0)));
        }

        private static void LogWarning(string code, string message, string doc, int line)
        {
            Result.Errors.Add(new Error(ErrorLevel.Warning, code, message, doc, new Range(line, 0)));
        }

        private static (string content, object file) ReadFile(string path, object relativeTo)
        {
            Debug.Assert(relativeTo is Document);

            var (error, content, child) = ((Document)relativeTo).TryResolveContent(path);

            if (error != null)
            {
                Result.Errors.Add(error);
            }

            t_dependencyMap.AddDependencyItem((Document)relativeTo, child, DependencyType.Inclusion);

            return (content, child);
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo)
        {
            Debug.Assert(relativeTo is Document);
            Debug.Assert(resultRelativeTo is Document);

            var self = (Document)relativeTo;
            var (error, link, fragment, child) = self.TryResolveHref(path, (Document)resultRelativeTo);

            if (error != null)
            {
                Result.Errors.Add(error);
            }

            if (child != null)
            {
                t_buildChild(child);
                t_dependencyMap.AddDependencyItem(self, child, HrefUtility.FragmentToDependencyType(fragment));
            }

            t_bookmarkValidator.AddBookmarkReference(self, child ?? self, fragment);

            return link;
        }

        private static string ResolveXref(string uid)
        {
            // TODO: implement xref resolve
            return null;
        }
    }
}
