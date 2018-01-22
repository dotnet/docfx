// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    using MarkdigEngine.Extensions;

    using Markdig;
    using Markdig.Syntax;
    using Markdig.Renderers;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdownEngine : IMarkdownEngine
    {
        private HashSet<string> _dependency;

        public MarkdownEngine(HashSet<string> dependency)
        {
            _dependency = dependency;
        }

        public string Markup(MarkdownContext context, MarkdownServiceParameters parameters)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var pipeline = CreatePipeline(context, parameters);

            return Markdown.ToHtml(context.Content, pipeline);
        }

        public MarkdownDocument Parse(MarkdownContext context, MarkdownServiceParameters parameters)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var pipeline = CreatePipeline(context, parameters);

            return Markdown.Parse(context.Content, pipeline);
        }

        public string Render(MarkdownDocument document, MarkdownContext context, MarkdownServiceParameters parameters)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var pipeline = CreatePipeline(context, parameters);

            using (var writer = new StringWriter())
            {
                var renderer = new HtmlRenderer(writer);
                pipeline.Setup(renderer);
                renderer.Render(document);
                writer.Flush();

                return writer.ToString();
            }
        }

        public void ReportDependency(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentException($"{nameof(file)} can't be null or empty.");
            }

            _dependency = _dependency ?? new HashSet<string>();
            _dependency.Add(file);
        }

        private MarkdownPipeline CreatePipeline(MarkdownContext context, MarkdownServiceParameters parameters)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var builder = new MarkdownPipelineBuilder()
                                .UseMarkdigAdvancedExtensions()
                                .UseDfmExtensions(this, context, parameters)
                                .RemoveUnusedExtensions();

            return builder.Build();
        }
    }
}
