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

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
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

        private readonly string _intermediateFolder;
        private readonly CompositionHost _container;
        private readonly PostProcessorsManager _postProcessorsManager;
        private readonly List<Assembly> _assemblyList;
        private readonly string _commitFromSHA;
        private readonly string _commitToSHA;
        private readonly string _templateHash;
        private readonly bool _cleanupCacheHistory;

        public DocumentBuilder(
            IEnumerable<Assembly> assemblies,
            ImmutableArray<string> postProcessorNames,
            string templateHash,
            string intermediateFolder = null,
            string commitFromSHA = null,
            string commitToSHA = null,
            bool cleanupCacheHistory = false)
        {
            Logger.LogVerbose("Loading plug-in...");
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

            _commitFromSHA = commitFromSHA;
            _commitToSHA = commitToSHA;
            _templateHash = templateHash;
            _intermediateFolder = intermediateFolder;
            _cleanupCacheHistory = cleanupCacheHistory;
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

            BuildInfo lastBuildInfo = null;
            var currentBuildInfo =
                new BuildInfo
                {
                    BuildStartTime = DateTime.UtcNow,
                    DocfxVersion = EnvironmentContext.Version,
                };

            try
            {
                using (new PerformanceScope("LoadLastBuildInfo"))
                {
                    lastBuildInfo = BuildInfo.Load(_intermediateFolder, true);
                }
                EnrichCurrentBuildInfo(currentBuildInfo, lastBuildInfo);

                _postProcessorsManager.IncrementalInitialize(_intermediateFolder, currentBuildInfo, lastBuildInfo, parameters[0].ForcePostProcess, parameters[0].MaxParallelism);

                var manifests = new List<Manifest>();
                bool transformDocument = false;
                if (parameters.All(p => p.Files.Count == 0))
                {
                    Logger.LogWarning(
                        $"No file found, nothing will be generated. Please make sure docfx.json is correctly configured.",
                        code: WarningCodes.Build.EmptyInputFiles);
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
                    FileAbstractLayerBuilder falBuilder;
                    if (_intermediateFolder == null)
                    {
                        falBuilder = FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory)
                            .WriteToRealFileSystem(parameter.OutputBaseDir);
                    }
                    else
                    {
                        falBuilder = FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory)
                            .WriteToLink(Path.Combine(_intermediateFolder, currentBuildInfo.DirectoryName));
                    }
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
                        manifests.Add(new Manifest());
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
                            manifests.Add(BuildCore(parameter, markdownServiceProvider, currentBuildInfo, lastBuildInfo));
                        }
                    }
                }
                if (noContentFound)
                {
                    Logger.LogWarning(
                        $"No content file found. Please make sure the content section of docfx.json is correctly configured.",
                        code: WarningCodes.Build.EmptyInputContents);
                }
                else if (emptyContentGroups.Count > 0)
                {
                    Logger.LogWarning(
                        $"No content file found in group: {string.Join(",", emptyContentGroups)}. Please make sure the content section of docfx.json is correctly configured.",
                        code: WarningCodes.Build.EmptyInputContents);
                }

                using (new LoggerPhaseScope("Postprocess", LogLevel.Verbose))
                {
                    var generatedManifest = ManifestUtility.MergeManifest(manifests);
                    generatedManifest.SitemapOptions = parameters.FirstOrDefault()?.SitemapOptions;
                    ManifestUtility.RemoveDuplicateOutputFiles(generatedManifest.Files);
                    ManifestUtility.ApplyLogCodes(generatedManifest.Files, logCodesLogListener.Codes);

                    // We can only globally shrink once to avoid invalid reference.
                    // Shrink multiplie times may remove files that are already linked in saved manifest.
                    if (_intermediateFolder != null)
                    {
                        // TODO: shrink here is not safe as post processor may update it.
                        //       should shrink once at last to handle everything, or make FAL support copy on writes
                        generatedManifest.Files.Shrink(_intermediateFolder, parameters[0].MaxParallelism);
                        currentBuildInfo.SaveVersionsManifet(_intermediateFolder);
                    }

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

                        // overwrite intermediate cache files
                        if (_intermediateFolder != null && transformDocument)
                        {
                            try
                            {
                                if (Logger.WarningCount >= Logger.WarningThrottling)
                                {
                                    currentBuildInfo.IsValid = false;
                                    currentBuildInfo.Message = $"Warning count {Logger.WarningCount} exceeds throttling {Logger.WarningThrottling}";
                                }
                                currentBuildInfo.Save(_intermediateFolder);
                                if (_cleanupCacheHistory)
                                {
                                    ClearCacheExcept(currentBuildInfo.DirectoryName);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning($"Error happened while saving cache. Message: {ex.Message}.");
                            }
                        }
                    }
                }
            }
            catch
            {
                // Leave cache folder there as it contains historical data
                // exceptions happens in this build does not corrupt the cache theoretically
                // however the cache file created by this build will never be cleaned up with DisableIncrementalFolderCleanup option
                if (_intermediateFolder != null && _cleanupCacheHistory)
                {
                    ClearCacheExcept(lastBuildInfo?.DirectoryName);
                }
                throw;
            }
            finally
            {
                Logger.UnregisterListener(logCodesLogListener);
            }

            IMarkdownServiceProvider GetMarkdownServiceProvider()
            {
                using (new PerformanceScope(nameof(GetMarkdownServiceProvider)))
                {
                    var result = CompositionContainer.GetExport<IMarkdownServiceProvider>(_container, parameters[0].MarkdownEngineName);
                    if (result == null)
                    {
                        Logger.LogError($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
                        throw new DocfxException($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
                    }
                    Logger.LogInfo($"Markdown engine is {parameters[0].MarkdownEngineName}", code: InfoCodes.Build.MarkdownEngineName);
                    return result;
                }
            }

            void EnrichCurrentBuildInfo(BuildInfo current, BuildInfo last)
            {
                current.CommitFromSHA = _commitFromSHA;
                current.CommitToSHA = _commitToSHA;
                if (_intermediateFolder != null)
                {
                    current.PluginHash = ComputePluginHash(_assemblyList);
                    current.TemplateHash = _templateHash;
                    if (!_cleanupCacheHistory && last != null)
                    {
                        // Reuse the directory for last incremental if cleanup is disabled
                        current.DirectoryName = last.DirectoryName;
                    }
                    else
                    {
                        current.DirectoryName = IncrementalUtility.CreateRandomDirectory(Environment.ExpandEnvironmentVariables(_intermediateFolder));
                    }
                }
            }
        }

        internal Manifest BuildCore(DocumentBuildParameters parameter, IMarkdownServiceProvider markdownServiceProvider, BuildInfo currentBuildInfo, BuildInfo lastBuildInfo)
        {
            using (var builder = new SingleDocumentBuilder
            {
                CurrentBuildInfo = currentBuildInfo,
                LastBuildInfo = lastBuildInfo,
                IntermediateFolder = _intermediateFolder,
                MetadataValidators = MetadataValidators.Concat(GetMetadataRules(parameter)).ToList(),
                Processors = Processors,
                MarkdownServiceProvider = markdownServiceProvider,
            })
            {
                return builder.Build(parameter);
            }
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

                    var markdigMarkdownService = CreateMarkdigMarkdownService(parameter);
                    foreach (var pair in resource.GetResourceStreams(@"^schemas/.*\.schema\.json"))
                    {
                        var fileName = Path.GetFileName(pair.Key);

                        using (new LoggerFileScope(fileName))
                        using (var stream = pair.Value)
                        using (var sr = new StreamReader(stream))
                        {
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
                                new FolderRedirectionManager(parameter.OverwriteFragmentsRedirectionRules));
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
                new CompositionContainer(CompositionContainer.DefaultContainer));
        }

        private void ClearCacheExcept(string subFolder)
        {
            string folder = Environment.ExpandEnvironmentVariables(_intermediateFolder);
            string except = string.IsNullOrEmpty(subFolder) ? string.Empty : Path.Combine(folder, subFolder);
            foreach (var f in Directory.EnumerateDirectories(folder))
            {
                if (FilePathComparer.OSPlatformSensitiveStringComparer.Equals(f, except))
                {
                    continue;
                }
                try
                {
                    Directory.Delete(f, true);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to delete cache files in path: {subFolder}. Details: {ex.Message}.");
                }
            }
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
                foreach (var item in
                    from assembly in assemblyList
                    select assembly.FullName + "@" + assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version.ToString() + "-" + assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version.ToString()
                    into item
                    orderby item
                    select item)
                {
                    builder.AppendLine(item);
                    Logger.LogVerbose($"New assembly info added: '{item}'");
                }
                result = builder.ToString().GetMd5String();
            }

            Logger.LogVerbose($"Plugin hash is '{result}'");
            return result;
        }

        public void Dispose()
        {
            _postProcessorsManager.Dispose();
        }
    }
}