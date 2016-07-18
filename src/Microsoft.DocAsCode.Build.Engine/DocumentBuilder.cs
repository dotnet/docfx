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

        public string IntermediateFolder { get; set; }

        private readonly List<PostProcessor> _postProcessors = new List<PostProcessor>();
        private readonly CompositionHost _container;
        private readonly BuildInfo _currentBuildInfo =
            new BuildInfo
            {
                BuildStartTime = DateTime.UtcNow,
                DocfxVersion = typeof(DocumentBuilderCore).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            };

        public DocumentBuilder(ImmutableArray<string> postProcessorNames, ImmutableDictionary<string, object> metadata, IEnumerable<Assembly> assemblies) :
            this(assemblies)
        {
            // For backward compatible, retain "_enableSearch" to globalMetadata though it's deprecated
            object value;
            if (metadata != null && metadata.TryGetValue("_enableSearch", out value))
            {
                var isSearchable = value as bool?;
                if (isSearchable.HasValue && isSearchable.Value && !postProcessorNames.Contains("ExtractSearchIndex"))
                {
                    postProcessorNames = postProcessorNames.Add("ExtractSearchIndex");
                }
            }

            _postProcessors = GetPostProcessor(postProcessorNames);
        }

        public DocumentBuilder(IEnumerable<Assembly> assemblies)
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
        }

        public Manifest Build(DocumentBuildParameters parameters)
        {
            using (var builder = new DocumentBuilderCore
            {
                Container = _container,
                IntermediateFolder = IntermediateFolder,
                MetadataValidators = MetadataValidators,
                Processors = Processors
            })
            {
                return builder.Build(parameters);
            }
        }

        public void PrepareMetadata(DocumentBuildParameters parameters)
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

        public void PostProcess(Manifest manifest, string outputDir)
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

        public void RemoveDuplicateOutputFiles(List<ManifestItem> manifestItems)
        {
            if (manifestItems == null)
            {
                throw new ArgumentNullException(nameof(manifestItems));
            }

            var itemsToRemove = new List<string>();
            foreach (var duplicates in from m in manifestItems
                                       from output in m.OutputFiles.Values
                                       group m.OriginalFile by output into g
                                       where g.Count() > 1
                                       select g)
            {
                Logger.LogWarning($"Overwrite occurs while input files \"{string.Join(", ", duplicates)}\" writing to the same output file \"{duplicates.Key}\"");
                itemsToRemove.AddRange(duplicates.Skip(1));
            }
            manifestItems.RemoveAll(m => itemsToRemove.Contains(m.OriginalFile));
        }

        private class PostProcessor
        {
            public string ContractName { get; set; }
            public IPostProcessor Processor { get; set; }
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

            configuration.WithAssembly(typeof(DocumentBuilderCore).Assembly);

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
        }
    }
}