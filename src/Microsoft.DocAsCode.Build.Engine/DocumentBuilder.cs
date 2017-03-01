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

    using Microsoft.DocAsCode.Build.Engine.Incrementals;
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
        private readonly BuildInfo _currentBuildInfo =
            new BuildInfo
            {
                BuildStartTime = DateTime.UtcNow,
                DocfxVersion = typeof(DocumentBuilder).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            };
        private readonly BuildInfo _lastBuildInfo;
        private readonly PostProcessorsManager _postProcessorsManager;

        public DocumentBuilder(
            IEnumerable<Assembly> assemblies,
            ImmutableArray<string> postProcessorNames,
            string templateHash,
            string intermediateFolder = null,
            string commitFromSHA = null,
            string commitToSHA = null)
        {
            Logger.LogVerbose("Loading plug-in...");
            using (new LoggerPhaseScope("ImportPlugins", LogLevel.Verbose))
            {
                var assemblyList = assemblies?.ToList() ?? new List<Assembly>();
                assemblyList.Add(typeof(DocumentBuilder).Assembly);
                _container = CompositionUtility.GetContainer(assemblyList);
                _container.SatisfyImports(this);
                _currentBuildInfo.CommitFromSHA = commitFromSHA;
                _currentBuildInfo.CommitToSHA = commitToSHA;
                if (intermediateFolder != null)
                {
                    _currentBuildInfo.PluginHash = ComputePluginHash(assemblyList);
                    _currentBuildInfo.TemplateHash = templateHash;
                    _currentBuildInfo.DirectoryName = IncrementalUtility.CreateRandomDirectory(Environment.ExpandEnvironmentVariables(intermediateFolder));
                }
            }
            Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
            foreach (var processor in Processors)
            {
                Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
            }
            _intermediateFolder = intermediateFolder;
            _lastBuildInfo = BuildInfo.Load(_intermediateFolder);
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

            var markdownServiceProvider = CompositionUtility.GetExport<IMarkdownServiceProvider>(_container, parameters[0].MarkdownEngineName);
            if (markdownServiceProvider == null)
            {
                Logger.LogError($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
                throw new DocfxException($"Unable to find markdown engine: {parameters[0].MarkdownEngineName}");
            }
            Logger.LogInfo($"Markdown engine is {parameters[0].MarkdownEngineName}");

            _postProcessorsManager.IncrementalInitialize(_intermediateFolder, _currentBuildInfo, _lastBuildInfo, parameters[0].ForcePostProcess);

            var manifests = new List<Manifest>();
            bool transformDocument = false;
            foreach (var parameter in parameters)
            {
                if (parameter.CustomLinkResolver != null)
                {
                    ICustomHrefGenerator chg;
                    if (_container.TryGetExport(parameter.CustomLinkResolver, out chg))
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
                        .WriteToLink(Path.Combine(_intermediateFolder, _currentBuildInfo.DirectoryName))
                        .Create();
                }
                if (parameter.Files.Count == 0)
                {
                    Logger.LogWarning(string.IsNullOrEmpty(parameter.VersionName)
                        ? "No files found, nothing is generated in default version."
                        : $"No files found, nothing is generated in version \"{parameter.VersionName}\".");
                    manifests.Add(new Manifest());
                    continue;
                }
                if (parameter.ApplyTemplateSettings.TransformDocument)
                {
                    transformDocument = true;
                }
                parameter.Metadata = _postProcessorsManager.PrepareMetadata(parameter.Metadata);
                if (!string.IsNullOrEmpty(parameter.VersionName))
                {
                    Logger.LogInfo($"Start building for version: {parameter.VersionName}");
                }
                manifests.Add(BuildCore(parameter, markdownServiceProvider));
            }

            using (new PerformanceScope("Postprocess"))
            {
                var generatedManifest = ManifestUtility.MergeManifest(manifests);
                ManifestUtility.RemoveDuplicateOutputFiles(generatedManifest.Files);

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
                    generatedManifest.Dereference(parameters[0].OutputBaseDir);
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
                        _currentBuildInfo.Save(_intermediateFolder);
                        if (_lastBuildInfo != null)
                        {
                            Directory.Delete(Path.Combine(Environment.ExpandEnvironmentVariables(_intermediateFolder), _lastBuildInfo.DirectoryName), true);
                        }
                    }
                }
            }
        }

        internal Manifest BuildCore(DocumentBuildParameters parameter, IMarkdownServiceProvider markdownServiceProvider)
        {
            using (var builder = new SingleDocumentBuilder
            {
                CurrentBuildInfo = _currentBuildInfo,
                LastBuildInfo = _lastBuildInfo,
                IntermediateFolder = _intermediateFolder,
                MetadataValidators = MetadataValidators.Concat(GetMetadataRules(parameter)).ToList(),
                Processors = Processors,
                MarkdownServiceProvider = markdownServiceProvider,
            })
            {
                return builder.Build(parameter);
            }
        }

        private IEnumerable<IInputMetadataValidator> GetMetadataRules(DocumentBuildParameters parameter)
        {
            try
            {
                var mvb = MarkdownValidatorBuilder.Create(_container, parameter.Files.DefaultBaseDir, parameter.TemplateDir);
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
            JsonUtility.Serialize(Constants.ManifestFileName, manifest);
            Logger.LogInfo($"Manifest file saved to {Constants.ManifestFileName}.");
        }

        private static string ComputePluginHash(List<Assembly> assemblyList)
        {
            if (assemblyList?.Count > 0)
            {
                var builder = new StringBuilder();
                foreach (var item in
                    from assembly in assemblyList
                    select assembly.FullName + "@" + assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version.ToString()
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