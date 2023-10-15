// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

#pragma warning disable CS0612 // Type or member is obsolete

public class SingleDocumentBuilder : IDisposable
{
    private const string PhaseName = "Build Document";
    private const string XRefMapFileName = "xrefmap.yml";

    public IEnumerable<IDocumentProcessor> Processors { get; set; }
    public IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

    public static ImmutableList<FileModel> Build(IDocumentProcessor processor, DocumentBuildParameters parameters, IMarkdownService markdownService)
    {
        var hostServiceCreator = new HostServiceCreator(null);
        var hostService = hostServiceCreator.CreateHostService(
            parameters,
            null,
            markdownService,
            null,
            processor,
            parameters.Files.EnumerateFiles());
        var phaseProcessor = new PhaseProcessor
        {
            Handlers =
                {
                    new CompilePhaseHandler(null),
                    new LinkPhaseHandler(null, null),
                }
        };
        phaseProcessor.Process(new List<HostService> { hostService }, parameters.MaxParallelism);
        return hostService.Models;
    }

    public Manifest Build(DocumentBuildParameters parameters)
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

        return BuildCore(parameters);
    }

    private Manifest BuildCore(DocumentBuildParameters parameters)
    {
        using (new LoggerPhaseScope(PhaseName, LogLevel.Verbose))
        {
            Directory.CreateDirectory(parameters.OutputBaseDir);

            var context = new DocumentBuildContext(parameters);

            // Start building document...
            List<HostService> hostServices = null;
            IHostServiceCreator hostServiceCreator = null;
            PhaseProcessor phaseProcessor = null;
            try
            {
                using var templateProcessor = parameters.TemplateManager?.GetTemplateProcessor(context, parameters.MaxParallelism)
                                              ?? new TemplateProcessor(new EmptyResourceReader(), context, 16);
                using (new LoggerPhaseScope("Prepare", LogLevel.Verbose))
                {
                    Prepare(
                        context,
                        templateProcessor,
                        out hostServiceCreator,
                        out phaseProcessor);
                }
                using (new LoggerPhaseScope("Load", LogLevel.Verbose))
                {
                    hostServices = GetInnerContexts(parameters, Processors, templateProcessor, hostServiceCreator);
                }

                templateProcessor.CopyTemplateResources(context.ApplyTemplateSettings);

                BuildCore(phaseProcessor, hostServices, context);

                var manifest = new Manifest(context.ManifestItems.Where(m => m.Output?.Count > 0))
                {
                    Xrefmap = ExportXRefMap(parameters, context),
                    SourceBasePath = StringExtension.ToNormalizedPath(EnvironmentContext.BaseDirectory),
                };
                manifest.Groups = new List<ManifestGroupInfo>
                {
                    new(parameters.GroupInfo)
                    {
                        XRefmap = (string)manifest.Xrefmap
                    }
                };
                return manifest;
            }
            finally
            {
                if (hostServices != null)
                {
                    foreach (var item in hostServices)
                    {
                        item.Dispose();
                    }
                }
            }
        }
    }

    private static void BuildCore(PhaseProcessor phaseProcessor, List<HostService> hostServices, DocumentBuildContext context)
    {
        phaseProcessor.Process(hostServices, context.MaxParallelism);
    }

    private List<HostService> GetInnerContexts(
        DocumentBuildParameters parameters,
        IEnumerable<IDocumentProcessor> processors,
        TemplateProcessor templateProcessor,
        IHostServiceCreator creator)
    {
        var markdownService = new MarkdigMarkdownService(
            new MarkdownServiceParameters
            {
                BasePath = parameters.Files.DefaultBaseDir,
                TemplateDir = parameters.TemplateDir,
                Extensions = parameters.MarkdownEngineParameters,
                Tokens = templateProcessor.Tokens.ToImmutableDictionary(),
            },
            configureMarkdig: parameters.ConfigureMarkdig);

        var files = (from file in parameters.Files.EnumerateFiles().AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism)
                     from p in (from processor in processors
                                let priority = processor.GetProcessingPriority(file)
                                where priority != ProcessingPriority.NotSupported
                                group processor by priority into ps
                                orderby ps.Key descending
                                select ps.ToList()).FirstOrDefault() ?? new List<IDocumentProcessor> { null }
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
            return (from processor in processors.AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism)
                    join item in toHandleItems.AsParallel() on processor equals item.Key into g
                    from item in g.DefaultIfEmpty()
                    where item != null && item.Any(s => s.Type != DocumentType.Overwrite) // when normal file exists then processing is needed
                    select LoggerPhaseScope.WithScope(
                        processor.Name,
                        LogLevel.Verbose,
                        () => creator.CreateHostService(
                            parameters,
                            templateProcessor,
                            markdownService,
                            MetadataValidators,
                            processor,
                            item)
                            )).ToList();
        }
        catch (AggregateException ex)
        {
            throw ex.GetBaseException();
        }
    }

    private static void Prepare(
        DocumentBuildContext context,
        TemplateProcessor templateProcessor,
        out IHostServiceCreator hostServiceCreator,
        out PhaseProcessor phaseProcessor)
    {
        hostServiceCreator = new HostServiceCreator(context);
        phaseProcessor = new PhaseProcessor
        {
            Handlers =
                {
                    new CompilePhaseHandler(context),
                    new LinkPhaseHandler(context, templateProcessor),
                }
        };
    }

    /// <summary>
    /// Export xref map file.
    /// </summary>
    private static string ExportXRefMap(DocumentBuildParameters parameters, DocumentBuildContext context)
    {
        Logger.LogVerbose("Exporting xref map...");
        var xrefMap = new XRefMap
        {
            References = (from xref in context.XRefSpecMap.Values.AsParallel().WithDegreeOfParallelism(parameters.MaxParallelism)
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
        using (new PerformanceScope("DisposeDocumentProcessors"))
        {
            foreach (var processor in Processors)
            {
                Logger.LogVerbose($"Disposing processor {processor.Name} ...");
                (processor as IDisposable)?.Dispose();
            }
        }
    }
}

