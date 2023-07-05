// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.MarkdigEngine.Extensions;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.MarkdigEngine;

public class MarkdigMarkdownService : IMarkdownService
{
    public string Name => "markdig";

    private readonly MarkdownServiceParameters _parameters;
    private readonly MarkdownValidatorBuilder _mvb;
    private readonly MarkdownContext _context;
    private readonly Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> _configureMarkdig;

    public MarkdigMarkdownService(
        MarkdownServiceParameters parameters,
        ICompositionContainer container = null,
        Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> configureMarkdig = null)
    {
        _parameters = parameters;
        _configureMarkdig = configureMarkdig;
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
        return Markup(content, filePath, false, false);
    }

    public MarkupResult Markup(string content, string filePath, bool enableValidation, bool multipleYamlHeader)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (filePath == null)
        {
            throw new ArgumentException("file path can't be null or empty.");
        }

        var pipeline = CreateMarkdownPipeline(isInline: false, enableValidation, multipleYamlHeader);

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

    private MarkdownPipeline CreateMarkdownPipeline(bool isInline, bool enableValidation, bool multipleYamlHeader = false)
    {
        var enableSourceInfo = _parameters?.Extensions?.EnableSourceInfo ?? true;

        var builder = new MarkdownPipelineBuilder();

        builder.UseDocfxExtensions(_context, _parameters.Extensions?.Alerts);
        builder.Extensions.Insert(0, new YamlHeaderExtension(_context) { AllowInMiddleOfDocument = multipleYamlHeader });

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

        if (_parameters?.Extensions?.MarkdigExtensions is { } extensions && extensions.Length > 0)
        {
            builder.UseOptionalExtensions(extensions);
        }

        if (_configureMarkdig != null)
        {
            builder = _configureMarkdig(builder);
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

    private string GetImageLink(string href, MarkdownObject origin, string altText) => GetLink(href, origin);

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
