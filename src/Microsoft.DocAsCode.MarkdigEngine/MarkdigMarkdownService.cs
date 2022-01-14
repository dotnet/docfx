// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using MarkdigEngine.Extensions;

    using Markdig;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Common;
    using System.Collections.Generic;

    public class MarkdigMarkdownService : IMarkdownService
    {
        public string Name => "markdig";

        private readonly MarkdownServiceParameters _parameters;
        private readonly MarkdownValidatorBuilder _mvb;
        private readonly MarkdownContext _context;

        public MarkdigMarkdownService(
            MarkdownServiceParameters parameters,
            ICompositionContainer container = null)
        {
            _parameters = parameters;
            _mvb = MarkdownValidatorBuilder.Create(parameters, container);
            _context = new MarkdownContext(
                key => _parameters.Tokens.TryGetValue(key, out var value) ? value : null,
                (code, message, origin, line) => Logger.LogInfo(message, null, InclusionContext.File.ToString(), line?.ToString(), code),
                (code, message, origin, line) => Logger.LogSuggestion(message, null, InclusionContext.File.ToString(), line?.ToString(), code),
                (code, message, origin, line) => Logger.LogWarning(message, null, InclusionContext.File.ToString(), line?.ToString(), code),
                (code, message, origin, line) => Logger.LogError(message, null, InclusionContext.File.ToString(), line?.ToString(), code),
                ReadFile,
                GetLink,
                GetImageLink);
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

            var pipeline = CreateMarkdownPipeline(isInline: false, enableValidation: enableValidation);

            using (InclusionContext.PushFile((RelativePath)filePath))
            {
                return new MarkupResult
                {
                    Html = Markdown.ToHtml(content, pipeline),
                    Dependency = InclusionContext.Dependencies.Select(file => (string)(RelativePath)file).ToImmutableArray()
                };
            }
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

            var pipeline = CreateMarkdownPipeline(isInline, enableValidation: false);

            using (InclusionContext.PushFile((RelativePath)filePath))
            {
                var document = Markdown.Parse(content, pipeline);
                document.SetData("filePath", filePath);

                return document;
            }
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

            if (!(document.GetData("filePath") is string filePath))
            {
                throw new ArgumentNullException("file path can't be found in AST.");
            }

            var pipeline = CreateMarkdownPipeline(isInline, enableValidation: false);

            using (InclusionContext.PushFile((RelativePath)filePath))
            {
                using var writer = new StringWriter();
                var renderer = new HtmlRenderer(writer);
                pipeline.Setup(renderer);
                renderer.Render(document);
                writer.Flush();

                return new MarkupResult
                {
                    Html = writer.ToString(),
                    Dependency = InclusionContext.Dependencies.Select(file => (string)(RelativePath)file).ToImmutableArray()
                };
            }
        }

        private MarkdownPipeline CreateMarkdownPipeline(bool isInline, bool enableValidation)
        {
            object enableSourceInfoObj = null;
            _parameters?.Extensions?.TryGetValue(Constants.EngineProperties.EnableSourceInfo, out enableSourceInfoObj);

            var enableSourceInfo = !(enableSourceInfoObj is bool enabled) || enabled;

            var builder = new MarkdownPipelineBuilder();

            builder.UseDocfxExtensions(_context);
            builder.Extensions.Insert(0, new YamlHeaderExtension(_context));

            if (enableSourceInfo)
            {
                builder.UseLineNumber(file => ((RelativePath)file).RemoveWorkingFolder());
            }

            if (enableValidation)
            {
                builder.Extensions.Add(new ValidationExtension(_mvb, _context));
            }

            if (isInline)
            {
                builder.UseInlineOnly();
            }

            object optionalExtensionsObj = null;
            if ((_parameters?.Extensions?.TryGetValue(Constants.EngineProperties.MarkdigExtensions, out optionalExtensionsObj) ?? false)
                && optionalExtensionsObj is IEnumerable<object> optionalExtensions)
            {
                builder.UseOptionalExtensions(optionalExtensions.Select(e => e as string).Where(e => e != null));
            }

            return builder.Build();
        }

        private (string content, object file) ReadFile(string path, MarkdownObject origin)
        {
            if (!PathUtility.IsRelativePath(path))
            {
                return (null, null);
            }

            var currentFilePath = ((RelativePath)InclusionContext.File).GetPathFromWorkingFolder();
            var includedFilePath = ((RelativePath)path).BasedOn(currentFilePath);
            var includedFilePathWithoutWorkingFolder = includedFilePath.RemoveWorkingFolder();
            var parentFileDirectoryToDocset = Path.GetDirectoryName(Path.Combine(_parameters.BasePath, ((RelativePath)InclusionContext.RootFile).RemoveWorkingFolder()));

            ReportDependency(includedFilePathWithoutWorkingFolder, parentFileDirectoryToDocset);
            if (!EnvironmentContext.FileAbstractLayer.Exists(includedFilePathWithoutWorkingFolder))
            {
                return (null, null);
            }

            var content = EnvironmentContext.FileAbstractLayer.ReadAllText(includedFilePathWithoutWorkingFolder);

            return (content, includedFilePath);
        }

        private static string GetLink(string path, MarkdownObject origin)
        {
            if (InclusionContext.IsInclude && RelativePath.IsRelativePath(path) && PathUtility.IsRelativePath(path) && !RelativePath.IsPathFromWorkingFolder(path) && !path.StartsWith("#", StringComparison.Ordinal))
            {
                return ((RelativePath)InclusionContext.File + (RelativePath)path).GetPathFromWorkingFolder();
            }
            return path;
        }

        private string GetImageLink(string href, MarkdownObject origin, string? altText) => GetLink(href, origin);

        private void ReportDependency(RelativePath filePathToDocset, string parentFileDirectoryToDocset)
        {
            var expectedPhysicalPath = EnvironmentContext.FileAbstractLayer.GetExpectedPhysicalPath(filePathToDocset);
            foreach (var physicalPath in expectedPhysicalPath)
            {
                var fallbackFileRelativePath = PathUtility.MakeRelativePath(parentFileDirectoryToDocset, physicalPath);
                InclusionContext.PushDependency((RelativePath)fallbackFileRelativePath);
            }
        }
    }
}
