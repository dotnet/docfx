// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

#pragma warning disable CS0612 // Type or member is obsolete

class SingleDocumentBuilder : IDisposable
{
    private const string XRefMapFileName = "xrefmap.yml";

    public IEnumerable<IDocumentProcessor> Processors { get; set; }
    public IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

    public static ImmutableList<FileModel> Build(
        IDocumentProcessor processor,
        DocumentBuildParameters parameters,
        IMarkdownService markdownService)
    {
        var hostServiceCreator = new HostServiceCreator(null);
        var hostService = hostServiceCreator.CreateHostService(
            parameters,
            null,
            markdownService,
            null,
            processor,
            parameters.Files.EnumerateFiles());

        new CompilePhaseHandler(null).Handle([hostService], parameters.MaxParallelism);
        new LinkPhaseHandler(null, null).Handle([hostService], parameters.MaxParallelism);
        return hostService.Models;
    }

    public Manifest Build(DocumentBuildParameters parameters, IMarkdownService markdownService, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.OutputBaseDir == null)
        {
            throw new ArgumentException("Output folder cannot be null.", nameof(parameters) + "." + nameof(parameters.OutputBaseDir));
        }
        if (parameters.Files == null)
        {
            throw new ArgumentException("Source files cannot be null.", nameof(parameters) + "." + nameof(parameters.Files));
        }
        if (parameters.MaxParallelism <= 0)
        {
            parameters.MaxParallelism = Environment.ProcessorCount;
        }
        parameters.Metadata ??= ImmutableDictionary<string, object>.Empty;

        Directory.CreateDirectory(parameters.OutputBaseDir);

        var context = new DocumentBuildContext(parameters, cancellationToken);

        // Start building document...
        var templateProcessor = parameters.TemplateManager?.GetTemplateProcessor(context, parameters.MaxParallelism)
                ?? new TemplateProcessor(new EmptyResourceReader(), context, 16);

        var hostServiceCreator = new HostServiceCreator(context);
        var hostServices = GetInnerContexts(parameters, Processors, templateProcessor, hostServiceCreator, markdownService, cancellationToken);

        templateProcessor.CopyTemplateResources(context.ApplyTemplateSettings);

        new CompilePhaseHandler(context).Handle(hostServices, parameters.MaxParallelism);
        new LinkPhaseHandler(context, templateProcessor).Handle(hostServices, parameters.MaxParallelism);

        var manifest = new Manifest(context.ManifestItems.Where(m => m.Output?.Count > 0))
        {
            Xrefmap = ExportXRefMap(parameters, context),
            SourceBasePath = StringExtension.ToNormalizedPath(EnvironmentContext.BaseDirectory),
        };
        manifest.Groups =
                [
                    new(parameters.GroupInfo)
                    {
                        XRefmap = (string)manifest.Xrefmap
                    }
                ];
        return manifest;
    }

    private List<HostService> GetInnerContexts(
        DocumentBuildParameters parameters,
        IEnumerable<IDocumentProcessor> processors,
        TemplateProcessor templateProcessor,
        HostServiceCreator creator,
        IMarkdownService markdownService,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var files = (from file in parameters.Files.EnumerateFiles().AsParallel()
                                                                   .WithDegreeOfParallelism(parameters.MaxParallelism)
                                                                   .WithCancellation(cancellationToken)
                     from p in (from processor in processors
                                let priority = processor.GetProcessingPriority(file)
                                where priority != ProcessingPriority.NotSupported
                                group processor by priority into ps
                                orderby ps.Key descending
                                select ps.ToList()).FirstOrDefault() ?? [null]
                     group file by p).ToList();

        var toHandleItems = files.Where(s => s.Key != null);
        var notToHandleItems = files
            .Where(s => s.Key == null)
            .SelectMany(s => s)
            .Where(s => s.Type != DocumentType.Overwrite &&
                !s.File.EndsWith(".yaml.md", StringComparison.OrdinalIgnoreCase) &&
                !s.File.EndsWith(".yml.md", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (notToHandleItems.Count > 0)
        {
            Logger.LogWarning(
                $"Unable to handle following files: {notToHandleItems.Select(s => s.File).ToDelimitedString()}. Do they miss `YamlMime` as the first line of file, e.g.: `### YamlMime:ManagedReference`?",
                code: WarningCodes.Build.UnknownContentType);
        }

        try
        {
            return (from processor in processors.AsParallel()
                                                .WithDegreeOfParallelism(parameters.MaxParallelism)
                                                .WithCancellation(cancellationToken)
                    join item in toHandleItems.AsParallel() on processor equals item.Key into g
                    from item in g.DefaultIfEmpty()
                    where item != null && item.Any(s => s.Type != DocumentType.Overwrite) // when normal file exists then processing is needed
                    select creator.CreateHostService(
                            parameters,
                            templateProcessor,
                            markdownService,
                            MetadataValidators,
                            processor,
                            item)).ToList();
        }
        catch (AggregateException ex)
        {
            throw ex.GetBaseException();
        }
    }

    /// <summary>
    /// Export xref map file.
    /// </summary>
    private static string ExportXRefMap(DocumentBuildParameters parameters, DocumentBuildContext context)
    {
        Logger.LogVerbose("Exporting xref map...");

        context.CancellationToken.ThrowIfCancellationRequested();

        var xrefMap = new XRefMap
        {
            References = (from xref in context.XRefSpecMap.Values.AsParallel()
                                                                 .WithDegreeOfParallelism(parameters.MaxParallelism)
                                                                 .WithCancellation(context.CancellationToken)
                          select new XRefSpec(xref)
                          {
                              Href = context.UpdateHref(xref.Href, RelativePath.WorkingFolder)
                          }).ToList(),
        };
        xrefMap.Sort();
        string xrefMapFileNameWithVersion = GetXrefMapFileNameWithGroup(parameters);
        YamlUtility.Serialize(
            Path.GetFullPath(Environment.ExpandEnvironmentVariables(Path.Combine(parameters.OutputBaseDir, xrefMapFileNameWithVersion))),
            xrefMap,
            YamlMime.XRefMap);
        Logger.LogInfo("XRef map exported.");
        return xrefMapFileNameWithVersion;
    }

    private static string GetXrefMapFileNameWithGroup(DocumentBuildParameters parameters)
    {
        if (!string.IsNullOrEmpty(parameters.GroupInfo?.Name))
        {
            return parameters.GroupInfo.Name + "." + XRefMapFileName;
        }
        if (!string.IsNullOrEmpty(parameters.VersionName))
        {
            return Uri.EscapeDataString(parameters.VersionName) + "." + XRefMapFileName;
        }
        return XRefMapFileName;
    }

    public void Dispose()
    {
        foreach (var processor in Processors)
        {
            Logger.LogVerbose($"Disposing processor {processor.Name} ...");
            (processor as IDisposable)?.Dispose();
        }
    }
}

