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
                (code, message, file, line) => Logger.LogWarning(message, null, file, line.ToString(), code),
                (code, message, file, line) => Logger.LogError(message, null, file, line.ToString(), code),
                ReadFile,
                GetLink);
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

            var filePath = document.GetData("filePath") as string;
            if (filePath == null)
            {
                throw new ArgumentNullException("file path can't be found in AST.");
            }

            var pipeline = CreateMarkdownPipeline(isInline, enableValidation: false);

            using (InclusionContext.PushFile((RelativePath)filePath))
            using (var writer = new StringWriter())
            {
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
            _parameters?.Extensions?.TryGetValue("EnableSourceInfo", out enableSourceInfoObj);

            var enabled = enableSourceInfoObj as bool?;
            var enableSourceInfo = enabled == null || enabled.Value;

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

            return builder.Build();
        }

        private static string GetLink(string path, object relativeTo, object resultRelativeTo)
        {
            if (InclusionContext.IsInclude && RelativePath.IsRelativePath(path) && PathUtility.IsRelativePath(path) && !RelativePath.IsPathFromWorkingFolder(path) && !path.StartsWith("#"))
            {
                return ((RelativePath)relativeTo + (RelativePath)path).GetPathFromWorkingFolder();
            }
            return path;
        }

        private (string content, object file) ReadFile(string path, object relativeTo)
        {
            if (!PathUtility.IsRelativePath(path))
            {
                return (null, null);
            }

            var currentFilePath = ((RelativePath)relativeTo).GetPathFromWorkingFolder();
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
