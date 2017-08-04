﻿// Copyright (c) Microsoft. All rights reserved.
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

            var markdownServiceProvider = CompositionContainer.GetExport<IMarkdownServiceProvider>(_container, parameters[0].MarkdownEngineName);
            if (markdownServiceProvider == null)
            {
                Logger.LogError($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
                throw new DocfxException($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
            }
            Logger.LogInfo($"Markdown engine is {parameters[0].MarkdownEngineName}");

            var logCodesLogListener = new LogCodesLogListener();
            Logger.RegisterListener(logCodesLogListener);

            // Load schema driven processor from template
            var sdps = LoadSchemaDrivenDocumentProcessors(parameters[0]).ToList();

            if (sdps.Count > 0)
            {
                Logger.LogInfo($"{sdps.Count()} schema driven document processor plug-in(s) loaded.");
                Processors = Processors.Union(sdps);
            }

            var currentBuildInfo =
                new BuildInfo
                {
                    BuildStartTime = DateTime.UtcNow,
                    DocfxVersion = EnvironmentContext.Version,
                };

            try
            {
                var lastBuildInfo = BuildInfo.Load(_intermediateFolder);

                currentBuildInfo.CommitFromSHA = _commitFromSHA;
                currentBuildInfo.CommitToSHA = _commitToSHA;
                if (_intermediateFolder != null)
                {
                    currentBuildInfo.PluginHash = ComputePluginHash(_assemblyList);
                    currentBuildInfo.TemplateHash = _templateHash;
                    if (!_cleanupCacheHistory && lastBuildInfo != null)
                    {
                        // Reuse the directory for last incremental if cleanup is disabled
                        currentBuildInfo.DirectoryName = lastBuildInfo.DirectoryName;
                    }
                    else
                    {
                        currentBuildInfo.DirectoryName = IncrementalUtility.CreateRandomDirectory(Environment.ExpandEnvironmentVariables(_intermediateFolder));
                    }
                }

                _postProcessorsManager.IncrementalInitialize(_intermediateFolder, currentBuildInfo, lastBuildInfo, parameters[0].ForcePostProcess, parameters[0].MaxParallelism);

                var manifests = new List<Manifest>();
                bool transformDocument = false;
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
                    if (_intermediateFolder == null)
                    {
                        EnvironmentContext.FileAbstractLayerImpl =
                            FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory)
                            .WriteToRealFileSystem(parameter.OutputBaseDir)
                            .Create();
                    }
                    else
                    {
                        EnvironmentContext.FileAbstractLayerImpl =
                            FileAbstractLayerBuilder.Default
                            .ReadFromRealFileSystem(EnvironmentContext.BaseDirectory)
                            .WriteToLink(Path.Combine(_intermediateFolder, currentBuildInfo.DirectoryName))
                            .Create();
                    }
                    if (parameter.ApplyTemplateSettings.TransformDocument)
                    {
                        transformDocument = true;
                    }

                    var versionMessageSuffix = string.IsNullOrEmpty(parameter.VersionName) ? string.Empty : $" in version \"{parameter.VersionName}\"";
                    if (parameter.Files.Count == 0)
                    {
                        Logger.LogWarning($"No file found, nothing will be generated{versionMessageSuffix}. Please make sure docfx.json is correctly configured.");
                        manifests.Add(new Manifest());
                    }
                    else
                    {
                        if (!parameter.Files.EnumerateFiles().Any(s => s.Type == DocumentType.Article))
                        {
                            Logger.LogWarning($"No content file found{versionMessageSuffix}. Please make sure the content section of docfx.json is correctly configured.");
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

                using (new LoggerPhaseScope("Postprocess", LogLevel.Verbose))
                {
                    var generatedManifest = ManifestUtility.MergeManifest(manifests);
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

                        // overwrite intermediate cache files
                        if (_intermediateFolder != null && transformDocument)
                        {
                            try
                            {
                                currentBuildInfo.Save(_intermediateFolder);
                                if (lastBuildInfo != null && _cleanupCacheHistory)
                                {
                                    ClearCacheWithNoThrow(lastBuildInfo.DirectoryName, true);
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
                    ClearCacheWithNoThrow(currentBuildInfo.DirectoryName, true);
                }
                throw;
            }
            finally
            {
                Logger.UnregisterListener(logCodesLogListener);
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

        private IEnumerable<IDocumentProcessor> LoadSchemaDrivenDocumentProcessors(DocumentBuildParameters parameter)
        {
            using (var resource = parameter?.TemplateManager?.CreateTemplateResource())
            {
                if (resource == null || resource.IsEmpty)
                {
                    yield break;
                }

                foreach (var pair in resource.GetResourceStreams(@"^schemas/.*\.schema\.json"))
                {
                    var fileName = Path.GetFileName(pair.Key);
                    using (var stream = pair.Value)
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var schema = DocumentSchema.Load(sr, fileName.Remove(fileName.Length - ".schema.json".Length));
                            var sdp = new SchemaDrivenDocumentProcessor(schema, new CompositionContainer(CompositionContainer.DefaultContainer));
                            Logger.LogVerbose($"\t{sdp.Name} with build steps ({string.Join(", ", from bs in sdp.BuildSteps orderby bs.BuildOrder select bs.Name)})");
                            yield return sdp;
                        }
                    }
                }
            }
        }

        private void ClearCacheWithNoThrow(string subFolder, bool recursive)
        {
            if (string.IsNullOrEmpty(subFolder))
            {
                return;
            }

            try
            {
                string fullPath = Path.Combine(Environment.ExpandEnvironmentVariables(_intermediateFolder), subFolder);
                Directory.Delete(fullPath, recursive);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to delete cache files in path: {subFolder}. Details: {ex.Message}.");
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
                }
                return builder.ToString().GetMd5String();
            }
            return string.Empty;
        }

        public void Dispose()
        {
            _postProcessorsManager.Dispose();
        }
    }
}