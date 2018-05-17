// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
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
        private static readonly IReadOnlyDictionary<string, string> s_markdownTokens = new Dictionary<string, string>
        {
            { "Note", "Note" },
            { "Tip", "Tip" },
            { "Warning", "Warning" },
            { "Important", "Important" },
            { "Caution", "Caution" },
        };

        public static (string html, JObject metadata) Markup(string markdown, Document file, Context context)
        {
            using (InclusionContext.PushFile(file))
            {
                var metadata = new JObject();
                var pipeline = CreateMarkdownPipeline(file, context, metadata);
                var html = Markdown.ToHtml(markdown, pipeline);
                return (html, metadata);
            }
        }

        private static MarkdownPipeline CreateMarkdownPipeline(Document file, Context context, JObject metadata)
        {
            var markdownContext = new MarkdownContext(
                s_markdownTokens,
                context.ReportWarning,
                context.ReportError,
                ReadFile,
                GetLink);

            return new MarkdownPipelineBuilder()
                .UseYamlFrontMatter()
                .UseDocfxExtensions(markdownContext)
                .Use(ExtractMetadataFromYamlHeader)
                .Build();

            (string content, object file) ReadFile(string path, object relativeTo)
            {
                Debug.Assert(relativeTo is Document);

                return ((Document)relativeTo).TryResolveContent(path);
            }

            string GetLink(string path, object relativeTo)
            {
                Debug.Assert(relativeTo is Document);

                var (link, _) = ((Document)relativeTo).TryResolveHref(path, file);

                return link;
            }

            void ExtractMetadataFromYamlHeader(MarkdownDocument document)
            {
                Visit(document, node =>
                {
                    if (node is YamlFrontMatterBlock yamlHeader)
                    {
                        try
                        {
                            var yamlHeaderObj = YamlUtility.Deserialize(yamlHeader.Lines.ToString());

                            if (!(yamlHeaderObj is JObject))
                            {
                                context.ReportWarning(Errors.YamlHeaderNotObject(file, isArray: yamlHeaderObj is JArray));
                            }
                            else
                            {
                                metadata.Merge(yamlHeaderObj);
                            }
                        }
                        catch (Exception ex)
                        {
                            context.ReportWarning(Errors.InvalidYamlHeader(file, ex));
                        }
                        return true;
                    }
                    return false;
                });
            }
        }

        /// <summary>
        /// Traverse the markdown object graph, returns true to stop the traversal.
        /// </summary>
        private static bool Visit(MarkdownObject obj, Func<MarkdownObject, bool> action)
        {
            if (obj == null)
                return true;

            if (action(obj))
                return true;

            if (obj is ContainerBlock block)
            {
                foreach (var child in block)
                {
                    if (Visit(child, action))
                    {
                        return true;
                    }
                }
            }
            else if (obj is LeafBlock leaf)
            {
                if (leaf.Inline != null)
                {
                    foreach (var child in leaf.Inline)
                    {
                        if (Visit(child, action))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (obj is ContainerInline inline)
            {
                foreach (var child in inline)
                {
                    if (Visit(child, action))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static MarkdownPipelineBuilder Use(this MarkdownPipelineBuilder builder, ProcessDocumentDelegate documentProcessed)
        {
            builder.Extensions.Add(new DelegatingExtension(pipeline => pipeline.DocumentProcessed += documentProcessed));
            return builder;
        }

        private class DelegatingExtension : IMarkdownExtension
        {
            private readonly Action<MarkdownPipelineBuilder> _setupPipeline;
            private readonly Action<IMarkdownRenderer> _setupRenderer;

            public DelegatingExtension(Action<MarkdownPipelineBuilder> setupPipeline = null, Action<IMarkdownRenderer> setupRenderer = null)
            {
                _setupPipeline = setupPipeline;
                _setupRenderer = setupRenderer;
            }

            public void Setup(MarkdownPipelineBuilder pipeline) => _setupPipeline?.Invoke(pipeline);

            public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer) => _setupRenderer?.Invoke(renderer);
        }
    }
}
