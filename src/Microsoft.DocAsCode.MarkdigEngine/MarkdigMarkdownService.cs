// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System;
    using System.Collections.Immutable;
    using System.IO;

    using MarkdigEngine.Extensions;

    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Plugins;

    public class MarkdigMarkdownService : IMarkdownService
    {
        public string Name => "markdig";

        private readonly MarkdownServiceParameters _parameters;
        private readonly MarkdownValidatorBuilder _mvb;

        public MarkdigMarkdownService(
            MarkdownServiceParameters parameters,
            ICompositionContainer container = null)
        {
            _parameters = parameters;
            _mvb = MarkdownValidatorBuilder.Create(parameters, container);
        }

        public MarkupResult Markup(string content, string filePath)
        {
            return Markup(content, filePath, false);
        }

        public MarkupResult Markup(string content, string filePath, bool enableValidation)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (filePath == null)
            {
                throw new ArgumentException("file path can't be null or empty.");
            }

            var options = CreateOptions(content, filePath, false, enableValidation);
            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(options)
                .Build();

            return new MarkupResult
            {
                Html = Markdown.ToHtml(content, pipeline),
                Dependency = options.Dependencies.ToImmutableArray()
            };
        }

        public MarkdownDocument Parse(string content, string filePath)
        {
            return Parse(content, filePath, false);
        }

        public MarkdownDocument Parse(string content, string filePath, bool isInline)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("file path can't be null or empty.");
            }

            var options = CreateOptions(content, filePath, isInline);
            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(options)
                .Build();

            var document = Markdown.Parse(content, pipeline);
            document.SetData("filePath", filePath);

            return document;
        }

        public MarkupResult Render(MarkdownDocument document)
        {
            return Render(document, false);
        }

        public MarkupResult Render(MarkdownDocument document, bool isInline)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var filePath = document.GetData("filePath") as string;
            if (filePath == null)
            {
                throw new ArgumentNullException("file path can't be found in AST.");
            }

            var options = CreateOptions(null, filePath, isInline);
            var pipeline = new MarkdownPipelineBuilder()
                .UseDocfxExtensions(options)
                .Build();

            using (var writer = new StringWriter())
            {
                var renderer = new HtmlRenderer(writer);
                pipeline.Setup(renderer);
                renderer.Render(document);
                writer.Flush();

                return new MarkupResult
                {
                    Html = writer.ToString(),
                    Dependency = options.Dependencies.ToImmutableArray(),
                };
            }
        }

        private MarkdownContext CreateOptions(string content, string filePath, bool isInline, bool enableValidation = false)
        {
            object enableSourceInfoObj = null;
            _parameters?.Extensions?.TryGetValue(LineNumberExtension.EnableSourceInfo, out enableSourceInfoObj);

            var enabled = enableSourceInfoObj as bool?;
            var enableSourceInfo = enabled == null || enabled.Value;

            return new MarkdownContext(
                _parameters.BasePath,
                filePath,
                isInline,
                null,
                null,
                enableSourceInfo,
                _parameters.Tokens,
                _mvb,
                enableValidation);
        }
    }
}
