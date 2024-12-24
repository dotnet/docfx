// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Composition.Hosting;
using System.Reflection;
using Docfx.Build.SchemaDriven;
using Docfx.Common;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.Engine;

public class DocumentBuilder : IDisposable
{
    [ImportMany]
    internal IEnumerable<IDocumentProcessor> Processors { get; set; }

    [ImportMany]
    internal IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

    private readonly CompositionHost _container;
    private readonly PostProcessorsManager _postProcessorsManager;

    public DocumentBuilder(IEnumerable<Assembly> assemblies, ImmutableArray<string> postProcessorNames)
    {
        Logger.LogVerbose("Loading plug-ins and post-processors...");
        var assemblyList = assemblies?.ToList() ?? [];
        assemblyList.Add(typeof(DocumentBuilder).Assembly);
        _container = CompositionContainer.GetContainer(assemblyList);
        _container.SatisfyImports(this);
        _postProcessorsManager = new PostProcessorsManager(_container, postProcessorNames);
    }

    public void Build(DocumentBuildParameters parameter)
    {
        Build(new DocumentBuildParameters[] { parameter }, parameter.OutputBaseDir);
    }

    public void Build(IList<DocumentBuildParameters> parameters, string outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        cancellationToken.ThrowIfCancellationRequested();

        if (parameters.Count == 0)
        {
            throw new ArgumentException("Parameters are empty.", nameof(parameters));
        }

        var markdownService = CreateMarkdigMarkdownService(parameters[0]);

        Processors = Processors.Append(new ApiPage.ApiPageDocumentProcessor(markdownService));

        Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
        foreach (var processor in Processors)
        {
            Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
        }

        // Load schema driven processor from template
        LoadSchemaDrivenDocumentProcessors(parameters[0]);

        var manifests = new List<Manifest>();
        if (parameters.All(p => p.Files.Count == 0))
        {
            Logger.LogSuggestion(
                "No file found, nothing will be generated. Please make sure docfx.json is correctly configured.",
                code: SuggestionCodes.Build.EmptyInputFiles);
        }

        var noContentFound = true;
        var emptyContentGroups = new List<string>();
        foreach (var parameter in parameters)
        {
            if (parameter.CustomLinkResolver != null)
            {
                if (_container.TryGetExport(parameter.CustomLinkResolver, out ICustomHrefGenerator chg))
                {
                    parameter.ApplyTemplateSettings.HrefGenerator = chg;
                }
                else
                {
                    Logger.LogWarning($"Custom href generator({parameter.CustomLinkResolver}) is not found.");
                }
            }
            FileAbstractLayerBuilder falBuilder = FileAbstractLayerBuilder.Default
                    .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory)
                    .WriteToRealFileSystem(parameter.OutputBaseDir);

            EnvironmentContext.FileAbstractLayerImpl = falBuilder.Create();

            if (parameter.Files.Count == 0)
            {
                manifests.Add(new Manifest { SourceBasePath = StringExtension.ToNormalizedPath(EnvironmentContext.BaseDirectory) });
            }
            else
            {
                if (!parameter.Files.EnumerateFiles().Any(static s => s.Type == DocumentType.Article))
                {
                    if (!string.IsNullOrEmpty(parameter.GroupInfo?.Name))
                    {
                        emptyContentGroups.Add(parameter.GroupInfo.Name);
                    }
                }
                else
                {
                    noContentFound = false;
                }

                parameter.Metadata = _postProcessorsManager.PrepareMetadata(parameter.Metadata);
                if (!string.IsNullOrEmpty(parameter.VersionName))
                {
                    Logger.LogInfo($"Start building for version: {parameter.VersionName}");
                }

                using var builder = new SingleDocumentBuilder
                {
                    MetadataValidators = MetadataValidators.ToList(),
                    Processors = Processors,
                };
                manifests.Add(builder.Build(parameter, markdownService, cancellationToken));
            }
        }
        if (noContentFound)
        {
            Logger.LogSuggestion(
                "No content file found. Please make sure the content section of docfx.json is correctly configured.",
                code: SuggestionCodes.Build.EmptyInputContents);
        }
        else if (emptyContentGroups.Count > 0)
        {
            Logger.LogSuggestion(
                $"No content file found in group: {string.Join(',', emptyContentGroups)}. Please make sure the content section of docfx.json is correctly configured.",
                code: SuggestionCodes.Build.EmptyInputContents);
        }

        var generatedManifest = ManifestUtility.MergeManifest(manifests);
        generatedManifest.Sitemap = parameters.FirstOrDefault()?.SitemapOptions;
        ManifestUtility.RemoveDuplicateOutputFiles(generatedManifest.Files);

        EnvironmentContext.FileAbstractLayerImpl =
            FileAbstractLayerBuilder.Default
            .ReadFromManifest(generatedManifest, parameters[0].OutputBaseDir)
            .WriteToManifest(generatedManifest, parameters[0].OutputBaseDir)
            .Create();

        _postProcessorsManager.Process(generatedManifest, outputDirectory);

        generatedManifest.Dereference(parameters[0].OutputBaseDir, parameters[0].MaxParallelism);

        // Save to manifest.json
        EnvironmentContext.FileAbstractLayerImpl =
            FileAbstractLayerBuilder.Default
            .ReadFromRealFileSystem(parameters[0].OutputBaseDir)
            .WriteToRealFileSystem(parameters[0].OutputBaseDir)
            .Create();

        generatedManifest.Files.Sort((a, b) => (a.SourceRelativePath ?? "").CompareTo(b.SourceRelativePath ?? ""));
        JsonUtility.Serialize("manifest.json", generatedManifest, indented: true);

        EnvironmentContext.FileAbstractLayerImpl = null;

        List<IDocumentProcessor> LoadSchemaDrivenDocumentProcessors(DocumentBuildParameters parameter)
        {
            var result = new List<IDocumentProcessor>();

            var resource = parameter?.TemplateManager?.CreateTemplateResource();
            if (resource == null || resource.IsEmpty)
            {
                return result;
            }

            foreach (var pair in resource.GetResources(@"^schemas/.*\.schema\.json"))
            {
                var fileName = Path.GetFileName(pair.Path);

                using (new LoggerFileScope(fileName))
                {
                    var schema = DocumentSchema.Load(pair.Content, fileName.Remove(fileName.Length - ".schema.json".Length));
                    var sdp = new SchemaDrivenDocumentProcessor(
                        schema,
                        new CompositionContainer(CompositionContainer.DefaultContainer),
                        markdownService);
                    Logger.LogVerbose($"\t{sdp.Name} with build steps ({string.Join(", ", from bs in sdp.BuildSteps orderby bs.BuildOrder select bs.Name)})");
                    result.Add(sdp);
                }
            }

            if (result.Count > 0)
            {
                Logger.LogInfo($"{result.Count} schema driven document processor plug-in(s) loaded.");
                Processors = Processors.Union(result);
            }
            return result;
        }
    }

    private static MarkdigMarkdownService CreateMarkdigMarkdownService(DocumentBuildParameters parameters)
    {
        var resource = parameters.TemplateManager?.CreateTemplateResource();

        return new MarkdigMarkdownService(
            new MarkdownServiceParameters
            {
                BasePath = parameters.Files.DefaultBaseDir,
                TemplateDir = parameters.TemplateDir,
                Extensions = parameters.MarkdownEngineParameters,
                Tokens = resource is null ? null : TemplateProcessorUtility.LoadTokens(resource)?.ToImmutableDictionary(),
            },
            parameters.ConfigureMarkdig);
    }

    public void Dispose()
    {
        _postProcessorsManager.Dispose();
    }
}
