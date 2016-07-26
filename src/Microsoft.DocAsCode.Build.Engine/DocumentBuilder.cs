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
    using System.Security.Cryptography;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    public class DocumentBuilder : IDisposable
    {
        [ImportMany]
        internal IEnumerable<IDocumentProcessor> Processors { get; set; }

        [ImportMany]
        internal IEnumerable<IInputMetadataValidator> MetadataValidators { get; set; }

        private readonly string _intermediateFolder;
        private readonly List<PostProcessor> _postProcessors = new List<PostProcessor>();
        private readonly CompositionHost _container;
        private readonly BuildInfo _currentBuildInfo =
            new BuildInfo
            {
                BuildStartTime = DateTime.UtcNow,
                DocfxVersion = typeof(DocumentBuilder).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            };
        private BuildInfo _lastBuildInfo;

        public DocumentBuilder(IEnumerable<Assembly> assemblies, ImmutableArray<string> postProcessorNames, string intermediateFolder = null)
        {
            Logger.LogVerbose("Loading plug-in...");
            var assemblyList = assemblies?.ToList();
            _container = GetContainer(assemblyList);
            _container.SatisfyImports(this);
            _currentBuildInfo.PluginHash = ComputePluginHash(assemblyList);
            Logger.LogInfo($"{Processors.Count()} plug-in(s) loaded.");
            foreach (var processor in Processors)
            {
                Logger.LogVerbose($"\t{processor.Name} with build steps ({string.Join(", ", from bs in processor.BuildSteps orderby bs.BuildOrder select bs.Name)})");
            }
            _postProcessors = GetPostProcessor(postProcessorNames);
            _intermediateFolder = intermediateFolder;
            _lastBuildInfo = LoadLastBuildInfo();
        }

        public Manifest Build(DocumentBuildParameters parameter)
        {
            using (var builder = new SingleDocumentBuilder
            {
                Container = _container,
                CurrentBuildInfo = _currentBuildInfo,
                IntermediateFolder = _intermediateFolder,
                MetadataValidators = MetadataValidators,
                Processors = Processors
            })
            {
                return builder.Build(parameter);
            }
        }

        public void Build(IEnumerable<DocumentBuildParameters> parameters, string outputDirectory)
        {
            var manifests = new List<Manifest>();
            foreach (var parameter in parameters)
            {
                if (parameter.Files.Count == 0)
                {
                    Logger.LogWarning(string.IsNullOrEmpty(parameter.VersionName)
                        ? "No files found, nothing is generated in default version."
                        : $"No files found, nothing is generated in version \"{parameter.VersionName}\".");
                    manifests.Add(new Manifest());
                    continue;
                }
                PrepareMetadata(parameter);
                if (!string.IsNullOrEmpty(parameter.VersionName))
                {
                    Logger.LogInfo($"Start building for version: {parameter.VersionName}");
                }
                manifests.Add(Build(parameter));
            }
            var generatedManifest = MergeManifest(manifests);

            RemoveDuplicateOutputFiles(generatedManifest.Files);
            PostProcess(generatedManifest, outputDirectory);

            // Save to manifest.json & .manifest(deprecated)
            SaveManifest(generatedManifest, outputDirectory);
        }

        private void PrepareMetadata(DocumentBuildParameters parameters)
        {
            foreach (var postProcessor in _postProcessors)
            {
                using (new LoggerPhaseScope(postProcessor.ContractName))
                {
                    parameters.Metadata = postProcessor.Processor.PrepareMetadata(parameters.Metadata);
                    if (parameters.Metadata == null)
                    {
                        throw new DocfxException($"Plugin {postProcessor.ContractName} should not return null metadata");
                    }
                }
            }
        }

        private void PostProcess(Manifest manifest, string outputDir)
        {
            // post process
            foreach (var postProcessor in _postProcessors)
            {
                using (new LoggerPhaseScope(postProcessor.ContractName))
                {
                    manifest = postProcessor.Processor.Process(manifest, outputDir);
                    if (manifest == null)
                    {
                        throw new DocfxException($"Plugin {postProcessor.ContractName} should not return null manifest");
                    }

                    // To make sure post processor won't generate duplicate output files
                    RemoveDuplicateOutputFiles(manifest.Files);
                }
            }
        }

        private void RemoveDuplicateOutputFiles(List<ManifestItem> manifestItems)
        {
            if (manifestItems == null)
            {
                throw new ArgumentNullException(nameof(manifestItems));
            }

            var itemsToRemove = new HashSet<string>();
            foreach (var duplicates in from m in manifestItems
                                       from output in m.OutputFiles.Values
                                       group m.SourceRelativePath by output into g
                                       where g.Count() > 1
                                       select g)
            {
                Logger.LogWarning($"Overwrite occurs while input files \"{string.Join(", ", duplicates)}\" writing to the same output file \"{duplicates.Key}\"");
                itemsToRemove.UnionWith(duplicates.Skip(1));
            }
            manifestItems.RemoveAll(m => itemsToRemove.Contains(m.SourceRelativePath));
        }

        private static Manifest MergeManifest(List<Manifest> manifests)
        {
            var xrefMaps = (from manifest in manifests
                            where manifest.XRefMap != null
                            select manifest.XRefMap).ToList();
            return new Manifest
            {
                Homepages = (from manifest in manifests
                             from homepage in manifest.Homepages ?? Enumerable.Empty<HomepageInfo>()
                             select homepage).Distinct().ToList(),
                Files = (from manifest in manifests
                         from file in manifest.Files ?? Enumerable.Empty<ManifestItem>()
                         select file).Distinct().ToList(),
                XRefMap = xrefMaps.Count <= 1 ? xrefMaps.FirstOrDefault() : xrefMaps,
                SourceBasePath = manifests.FirstOrDefault()?.SourceBasePath
            };
        }

        private static void SaveManifest(Manifest manifest, string outputDirectory)
        {
            // TODO: Keep .manifest for backward-compatability, will remove next sprint
            var manifestPath = Path.Combine(outputDirectory ?? string.Empty, Constants.ObsoleteManifestFileName);
            var deprecatedManifest = Transform(manifest.Files);
            JsonUtility.Serialize(manifestPath, deprecatedManifest);

            var manifestJsonPath = Path.Combine(outputDirectory ?? string.Empty, Constants.ManifestFileName);
            JsonUtility.Serialize(manifestJsonPath, manifest);
            Logger.LogInfo($"Manifest file saved to {manifestJsonPath}.");
        }

        private static List<DeprecatedManifestItem> Transform(List<ManifestItem> manifest)
        {
            return manifest.Select(item => new DeprecatedManifestItem
            {
                DocumentType = item.DocumentType,
                OriginalFile = item.OriginalFile,
                OutputFiles = item.OutputFiles.ToDictionary(k => k.Key, k => k.Value.RelativePath),
                Metadata = item.Metadata,
            }).ToList();
        }

        private BuildInfo LoadLastBuildInfo()
        {
            if (_intermediateFolder != null &&
                File.Exists(Path.Combine(_intermediateFolder, BuildInfo.FileName)))
            {
                try
                {
                    return JsonUtility.Deserialize<BuildInfo>(Path.Combine(_intermediateFolder, BuildInfo.FileName));
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private List<PostProcessor> GetPostProcessor(ImmutableArray<string> processors)
        {
            var processorList = new List<PostProcessor>();
            foreach (var processor in processors)
            {
                var p = GetExport(typeof(IPostProcessor), processor) as IPostProcessor;
                if (p != null)
                {
                    processorList.Add(new PostProcessor
                    {
                        ContractName = processor,
                        Processor = p
                    });
                    Logger.LogInfo($"Post processor {processor} loaded.");
                }
                else
                {
                    Logger.LogWarning($"Can't find the post processor: {processor}");
                }
            }
            return processorList;
        }

        private object GetExport(Type type, string name)
        {
            object exportedObject = null;
            try
            {
                exportedObject = _container.GetExport(type, name);
            }
            catch (CompositionFailedException ex)
            {
                Logger.LogWarning($"Can't import: {name}, {ex}");
            }
            return exportedObject;
        }

        private static CompositionHost GetContainer(IEnumerable<Assembly> assemblies)
        {
            var configuration = new ContainerConfiguration();

            configuration.WithAssembly(typeof(DocumentBuilder).Assembly);

            if (assemblies != null)
            {
                foreach (var assembly in assemblies)
                {
                    if (assembly != null)
                    {
                        configuration.WithAssembly(assembly);
                    }
                }
            }

            try
            {
                return configuration.CreateContainer();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Logger.LogError(
                    $"Error when get composition container: {ex.Message}, loader exceptions: {(ex.LoaderExceptions != null ? string.Join(", ", ex.LoaderExceptions.Select(e => e.Message)) : "none")}");
                throw;
            }
        }

        private static string ComputePluginHash(List<Assembly> assemblyList)
        {
            if (assemblyList?.Count > 0)
            {
                using (var ms = new MemoryStream())
                using (var writer = new StreamWriter(ms))
                {
                    foreach (var item in
                        from assembly in assemblyList
                        select assembly.FullName + "@" + assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version.ToString()
                        into item
                        orderby item
                        select item)
                    {
                        writer.WriteLine(item);
                    }
                    writer.Flush();
                    ms.Seek(0, SeekOrigin.Begin);
                    return Convert.ToBase64String(MD5.Create().ComputeHash(ms));
                }
            }
            return string.Empty;
        }

        public void Dispose()
        {
            foreach (var processor in _postProcessors)
            {
                Logger.LogVerbose($"Disposing processor {processor.ContractName} ...");
                (processor.Processor as IDisposable)?.Dispose();
            }
            if (_intermediateFolder != null)
            {
                JsonUtility.Serialize(
                    Path.Combine(_intermediateFolder, BuildInfo.FileName),
                    _currentBuildInfo);
            }
        }

        private sealed class PostProcessor
        {
            public string ContractName { get; set; }
            public IPostProcessor Processor { get; set; }
        }
    }
}