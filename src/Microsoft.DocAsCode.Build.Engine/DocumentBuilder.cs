// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Composition.Hosting;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Build.SchemaDriven;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.MarkdigEngine;
    using Microsoft.DocAsCode.Plugins;

    public class DocumentBuilder : IDisposable
    {
        [ImportMany]
        internal IEnumerable<IDocumentProcessor> Processors { get; set; }

        [ImportMany]
        internal IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

        private readonly CompositionHost _container;
        private readonly PostProcessorsManager _postProcessorsManager;
        private readonly List<Assembly> _assemblyList;

        public DocumentBuilder(IEnumerable<Assembly> assemblies, ImmutableArray<string> postProcessorNames)
        {
            Logger.LogVerbose("Loading plug-ins and post-processors...");
            using (new LoggerPhaseScope("ImportPlugins", LogLevel.Verbose))
            {
                var assemblyList = assemblies?.ToList() ?? new List<Assembly>();
                assemblyList.Add(typeof(DocumentBuilder).Assembly);
                _container = CompositionContainer.GetContainer(assemblyList);
                _container.SatisfyImports(this);
                _assemblyList = assemblyList;
            }
            Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
            foreach (var processor in Processors)
            {
                Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
            }
            _postProcessorsManager = new PostProcessorsManager(_container, postProcessorNames);
        }

        public void Build(DocumentBuildParameters parameter)
        {
            Build(new DocumentBuildParameters[] { parameter }, parameter.OutputBaseDir);
        }

        public void Build(IList<DocumentBuildParameters> parameters, string outputDirectory)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }
            if (parameters.Count == 0)
            {
                throw new ArgumentException("Parameters are empty.", nameof(parameters));
            }

            var markdownServiceProvider = GetMarkdownServiceProvider();
            var logCodesLogListener = new LogCodesLogListener();
            Logger.RegisterListener(logCodesLogListener);

            // Load schema driven processor from template
            var sdps = LoadSchemaDrivenDocumentProcessors(parameters[0]).ToList();

            try
            {
                var manifests = new List<Manifest>();
                bool transformDocument = false;
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

                    if (!string.IsNullOrEmpty(parameter.FALName))
                    {
                        if (_container.TryGetExport<IInputFileAbstractLayerBuilderProvider>(
                            parameter.FALName, out var provider))
                        {
                            falBuilder = provider.Create(falBuilder, parameter);
                        }
                        else
                        {
                            Logger.LogWarning($"Input fal builder provider not found, name: {parameter.FALName}.");
                        }
                    }
                    EnvironmentContext.FileAbstractLayerImpl = falBuilder.Create();
                    if (parameter.ApplyTemplateSettings.TransformDocument)
                    {
                        transformDocument = true;
                    }

                    if (parameter.Files.Count == 0)
                    {
                        manifests.Add(new Manifest() { SourceBasePath = StringExtension.ToNormalizedPath(EnvironmentContext.BaseDirectory) });
                    }
                    else
                    {
                        if (!parameter.Files.EnumerateFiles().Any(s => s.Type == DocumentType.Article))
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

                        using (new LoggerPhaseScope("BuildCore"))
                        {
                            manifests.Add(BuildCore(parameter, markdownServiceProvider));
                        }
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
                        $"No content file found in group: {string.Join(",", emptyContentGroups)}. Please make sure the content section of docfx.json is correctly configured.",
                        code: SuggestionCodes.Build.EmptyInputContents);
                }

                using (new LoggerPhaseScope("Postprocess", LogLevel.Verbose))
                {
                    var generatedManifest = ManifestUtility.MergeManifest(manifests);
                    generatedManifest.SitemapOptions = parameters.FirstOrDefault()?.SitemapOptions;
                    ManifestUtility.RemoveDuplicateOutputFiles(generatedManifest.Files);
                    ManifestUtility.ApplyLogCodes(generatedManifest.Files, logCodesLogListener.Codes);

                    EnvironmentContext.FileAbstractLayerImpl =
                        FileAbstractLayerBuilder.Default
                        .ReadFromManifest(generatedManifest, parameters[0].OutputBaseDir)
                        .WriteToManifest(generatedManifest, parameters[0].OutputBaseDir)
                        .Create();
                    using (new PerformanceScope("Process"))
                    {
                        _postProcessorsManager.Process(generatedManifest, outputDirectory);
                    }

                    using (new PerformanceScope("Dereference"))
                    {
                        if (parameters[0].KeepFileLink)
                        {
                            var count = (from f in generatedManifest.Files
                                         from o in f.OutputFiles
                                         select o.Value into v
                                         where v.LinkToPath != null
                                         select v).Count();
                            if (count > 0)
                            {
                                Logger.LogInfo($"Skip dereferencing {count} files.");
                            }
                        }
                        else
                        {
                            generatedManifest.Dereference(parameters[0].OutputBaseDir, parameters[0].MaxParallelism);
                        }
                    }

                    using (new PerformanceScope("SaveManifest"))
                    {
                        // Save to manifest.json
                        EnvironmentContext.FileAbstractLayerImpl =
                            FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(parameters[0].OutputBaseDir)
                            .WriteToRealFileSystem(parameters[0].OutputBaseDir)
                            .Create();
                        SaveManifest(generatedManifest);
                    }

                    using (new PerformanceScope("Cleanup"))
                    {
                        EnvironmentContext.FileAbstractLayerImpl = null;
                    }
                }
            }
            finally
            {
                Logger.UnregisterListener(logCodesLogListener);
            }

            IMarkdownServiceProvider GetMarkdownServiceProvider()
            {
                var markdownEngineName = parameters[0].MarkdownEngineName;
                if (markdownEngineName == "markdig")
                {
                    return new MarkdigServiceProvider
                    {
                        Container = _container.GetExport<ICompositionContainer>(),
                        ConfigureMarkdig = parameters[0].ConfigureMarkdig
                    };
                }
            
                var result = CompositionContainer.GetExport<IMarkdownServiceProvider>(_container, markdownEngineName);
                    if (result == null)
                    {
                    Logger.LogError($"Unable to find markdown engine: {markdownEngineName}");
                    throw new DocfxException($"Unable to find markdown engine: {markdownEngineName}");
                    }
                Logger.LogInfo($"Markdown engine is {markdownEngineName}", code: "MarkdownEngineName");
                    return result;
            }
        }

        internal Manifest BuildCore(DocumentBuildParameters parameter, IMarkdownServiceProvider markdownServiceProvider)
        {
            using var builder = new SingleDocumentBuilder
            {
                MetadataValidators = MetadataValidators.Concat(GetMetadataRules(parameter)).ToList(),
                Processors = Processors,
                MarkdownServiceProvider = markdownServiceProvider,
            };
            return builder.Build(parameter);
        }

        private List<IDocumentProcessor> LoadSchemaDrivenDocumentProcessors(DocumentBuildParameters parameter)
        {
            using (new LoggerPhaseScope(nameof(LoadSchemaDrivenDocumentProcessors)))
            {
                var result = new List<IDocumentProcessor>();

                SchemaValidateService.RegisterLicense(parameter.SchemaLicense);
                using (var resource = parameter?.TemplateManager?.CreateTemplateResource())
                {
                    if (resource == null || resource.IsEmpty)
                    {
                        return result;
                    }

                    var siteHostName = TryGetPublishTargetSiteHostNameFromEnvironment();
                    var markdigMarkdownService = CreateMarkdigMarkdownService(parameter);
                    foreach (var pair in resource.GetResourceStreams(@"^schemas/.*\.schema\.json"))
                    {
                        var fileName = Path.GetFileName(pair.Key);

                        using (new LoggerFileScope(fileName))
                        {
                            using var stream = pair.Value;
                            using var sr = new StreamReader(stream);
                            DocumentSchema schema;
                            try
                            {
                                schema = DocumentSchema.Load(sr, fileName.Remove(fileName.Length - ".schema.json".Length));
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e.Message);
                                throw;
                            }
                            var sdp = new SchemaDrivenDocumentProcessor(
                                schema,
                                new CompositionContainer(CompositionContainer.DefaultContainer),
                                markdigMarkdownService,
                                new FolderRedirectionManager(parameter.OverwriteFragmentsRedirectionRules),
                                siteHostName);
                            Logger.LogVerbose($"\t{sdp.Name} with build steps ({string.Join(", ", from bs in sdp.BuildSteps orderby bs.BuildOrder select bs.Name)})");
                            result.Add(sdp);
                        }
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

        private MarkdigMarkdownService CreateMarkdigMarkdownService(DocumentBuildParameters parameters)
        {
            var resourceProvider = parameters.TemplateManager?.CreateTemplateResource();

            return new MarkdigMarkdownService(
                new MarkdownServiceParameters
                {
                    BasePath = parameters.Files.DefaultBaseDir,
                    TemplateDir = parameters.TemplateDir,
                    Extensions = parameters.MarkdownEngineParameters,
                    Tokens = TemplateProcessorUtility.LoadTokens(resourceProvider)?.ToImmutableDictionary(),
                },
                new CompositionContainer(CompositionContainer.DefaultContainer),
                parameters.ConfigureMarkdig);
        }

        private IEnumerable<IInputMetadataValidator> GetMetadataRules(DocumentBuildParameters parameter)
        {
            try
            {
                var mvb = MarkdownValidatorBuilder.Create(new CompositionContainer(), parameter.Files.DefaultBaseDir, parameter.TemplateDir);
                return mvb.GetEnabledMetadataRules().ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Fail to init markdown style, details:{Environment.NewLine}{ex.Message}");
                return Enumerable.Empty<IInputMetadataValidator>();
            }
        }

        private static void SaveManifest(Manifest manifest)
        {
            JsonUtility.Serialize(Constants.ManifestFileName, manifest, Formatting.Indented);
            Logger.LogInfo($"Manifest file saved to {Constants.ManifestFileName}.");
        }

        private static string ComputePluginHash(List<Assembly> assemblyList)
        {
            Logger.LogVerbose("Calculating plugin hash...");

            var result = string.Empty;
            if (assemblyList?.Count > 0)
            {
                var builder = new StringBuilder();
                foreach (var assembly in
                    from assembly in assemblyList
                    orderby assembly.FullName
                    select assembly)
                {
                    var hashPart = assembly.FullName;
                    builder.AppendLine(hashPart);
                    var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                    Logger.LogVerbose($"New assembly info added: '{hashPart}'. Detailed file version: '{fileVersion}'");
                }
                result = HashUtility.GetSha256HashString(builder.ToString());
            }

            Logger.LogVerbose($"Plugin hash is '{result}'");
            return result;
        }

        private static string TryGetPublishTargetSiteHostNameFromEnvironment()
        {
            string metadataString = Environment.GetEnvironmentVariable(Constants.OPSEnvironmentVariable.SystemMetadata);

            if (metadataString != null)
            {
                var metadata = JsonUtility.FromJsonString<Dictionary<string, object>>(metadataString)?.ToImmutableDictionary();
                if (metadata.TryGetValue(Constants.OPSEnvironmentVariable.OpPublishTargetSiteHostName, out object publishTargetSiteHostName))
                {
                    return (string)publishTargetSiteHostName;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _postProcessorsManager.Dispose();
        }
    }
}
