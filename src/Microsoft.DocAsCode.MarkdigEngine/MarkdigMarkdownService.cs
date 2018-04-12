// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    using MarkdigEngine.Extensions;

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
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (filePath == null)
            {
                throw new ArgumentException("file path can't be null or empty.");
            }

            var dependency = new HashSet<string>();
            var engine = CreateEngine(dependency);

            var context = CreateContext(content, filePath, false);

            return new MarkupResult
            {
                Html = engine.Markup(context, _parameters),
                Dependency = dependency.ToImmutableArray()
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

            var engine = CreateEngine(new HashSet<string>());
            var context = CreateContext(content, filePath, isInline);

            var document = engine.Parse(context, _parameters);
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

            var context = CreateContext(null, filePath, isInline);

            var dependency = new HashSet<string>();
            var engine = CreateEngine(dependency);

            return new MarkupResult
            {
                Html = engine.Render(document, context, _parameters),
                Dependency = dependency.ToImmutableArray()
            };
        }

        private MarkdownEngine CreateEngine(HashSet<string> dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentNullException(nameof(dependency));
            }

            return new MarkdownEngine(dependency);
        }

        private MarkdownContext CreateContext(string content, string filePath, bool isInline)
        {
            return new MarkdownContextBuilder()
                            .WithFilePath(filePath)
                            .WithBasePath(_parameters.BasePath)
                            .WithMvb(_mvb)
                            .WithContent(content)
                            .WithIsInline(isInline)
                            .Build();
        }
    }
}
