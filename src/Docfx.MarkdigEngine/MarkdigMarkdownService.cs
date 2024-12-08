// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;
using Docfx.MarkdigEngine.Extensions;
using Docfx.Plugins;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using CollectionExtensions = System.Collections.Generic.CollectionExtensions;

namespace Docfx.MarkdigEngine;

public class MarkdigMarkdownService : IMarkdownService
{
    public string Name => "markdig";

    private readonly MarkdownServiceParameters _parameters;
    private readonly MarkdownContext _context;
    private readonly Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> _configureMarkdig;

    public MarkdigMarkdownService(
        MarkdownServiceParameters parameters,
        Func<MarkdownPipelineBuilder, MarkdownPipelineBuilder> configureMarkdig = null)
    {
        _parameters = parameters;
        _configureMarkdig = configureMarkdig;
        _context = new MarkdownContext(
            key => CollectionExtensions.GetValueOrDefault(_parameters.Tokens, key),
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

    public MarkupResult Markup(string content, string filePath, bool multipleYamlHeader)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var pipeline = CreateMarkdownPipeline(isInline: false, multipleYamlHeader);

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
        ArgumentNullException.ThrowIfNull(content);

        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("file path can't be null or empty.");
        }

        var pipeline = CreateMarkdownPipeline(isInline);

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
        ArgumentNullException.ThrowIfNull(document);

        if (document.GetData("filePath") is not string filePath)
        {
            throw new ArgumentNullException(nameof(document), "file path can't be found in AST.");
        }

        var pipeline = CreateMarkdownPipeline(isInline);

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

    private MarkdownPipeline CreateMarkdownPipeline(bool isInline, bool multipleYamlHeader = false)
    {
        var enableSourceInfo = _parameters?.Extensions?.EnableSourceInfo ?? true;

        var builder = new MarkdownPipelineBuilder();

        builder.UseDocfxExtensions(_context, _parameters?.Extensions?.Alerts, _parameters?.Extensions?.PlantUml);
        builder.Extensions.Insert(0, new YamlHeaderExtension(_context) { AllowInMiddleOfDocument = multipleYamlHeader });

        if (enableSourceInfo)
        {
            builder.UseLineNumber(file => ((RelativePath)file).RemoveWorkingFolder());
        }

        if (isInline)
        {
            builder.UseInlineOnly();
        }

        if (_parameters?.Extensions?.MarkdigExtensions is { Length: > 0 } extensions)
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
        if (InclusionContext.IsInclude && RelativePath.IsRelativePath(path) && PathUtility.IsRelativePath(path) && !RelativePath.IsPathFromWorkingFolder(path) && !path.StartsWith('#'))
        {
            return ((RelativePath)InclusionContext.File + (RelativePath)path).GetPathFromWorkingFolder();
        }
        return path;
    }

    private static string GetImageLink(string href, MarkdownObject origin, string altText) => GetLink(href, origin);

    private static void ReportDependency(RelativePath filePathToDocset, string parentFileDirectoryToDocset)
    {
        var physicalPath = EnvironmentContext.FileAbstractLayer.GetExpectedPhysicalPath(filePathToDocset);
        if (physicalPath is not null)
        {
            var fallbackFileRelativePath = PathUtility.MakeRelativePath(parentFileDirectoryToDocset, physicalPath);
            InclusionContext.PushDependency((RelativePath)fallbackFileRelativePath);
        }
    }
}
