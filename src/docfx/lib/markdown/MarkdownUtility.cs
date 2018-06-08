// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Markdig;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Converts markdown to html
    /// </summary>
    internal static class MarkdownUtility
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

        public static (string html, MarkupResult result) Markup(string markdown, Document file, Action<Document> buildChild)
        {
            var errors = new List<DocfxException>();
            var metadata = new StrongBox<JObject>();
            var title = new StrongBox<string>();
            var hasHtml = new StrongBox<bool>();

            var markdownContext = new MarkdownContext(s_markdownTokens, LogWarning, LogError, ReadFile, GetLink);

            var pipeline = new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .UseExtractYamlHeader(file, errors, metadata)
                .UseExtractTitle(title)
                .UseResolveHtmlLinks(markdownContext, hasHtml)
                .Build();

            using (InclusionContext.PushFile(file))
            {
                var html = Markdown.ToHtml(markdown, pipeline);

                var result = new MarkupResult
                {
                    Title = title.Value,
                    HasHtml = hasHtml.Value,
                    Metadata = metadata.Value,
                    Errors = errors,
                };

                return (html, result);
            }

            void LogError(string code, string message, string doc, int line)
            {
                errors.Add(new DocfxException(ReportLevel.Error, code, message, doc, line));
            }

            void LogWarning(string code, string message, string doc, int line)
            {
                errors.Add(new DocfxException(ReportLevel.Warning, code, message, doc, line));
            }

            (string content, object file) ReadFile(string path, object relativeTo)
            {
                Debug.Assert(relativeTo is Document);

                var (error, content, child) = ((Document)relativeTo).TryResolveContent(path);

                if (error != null)
                {
                    errors.Add(error);
                }

                return (content, child);
            }

            string GetLink(string path, object relativeTo)
            {
                Debug.Assert(relativeTo is Document);

                var (error, link, child) = ((Document)relativeTo).TryResolveHref(path, file);

                if (error != null)
                {
                    errors.Add(error);
                }

                if (child != null)
                {
                    buildChild(child);
                }

                return link;
            }
        }
    }
}
