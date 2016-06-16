// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Reflection;
    using System.Runtime.Remoting.Lifetime;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Build.ResourceFiles;
    using Microsoft.DocAsCode.Build.RestApi;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Serializable]
    internal sealed class DocumentBuilderWrapper
    {
        private readonly string _pluginDirectory;
        private readonly string _baseDirectory;
        private readonly string _outputDirectory;
        private readonly BuildJsonConfig _config;
        private readonly CrossAppDomainListener _listener;
        private readonly TemplateManager _manager;
        private readonly LogLevel _logLevel;

        public DocumentBuilderWrapper(BuildJsonConfig config, TemplateManager manager, string baseDirectory, string outputDirectory, string pluginDirectory, CrossAppDomainListener listener)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _pluginDirectory = pluginDirectory;
            _baseDirectory = baseDirectory;
            _outputDirectory = outputDirectory;
            _config = config;
            _listener = listener;
            _manager = manager;
            _logLevel = Logger.LogLevelThreshold;
        }

        public void BuildDocument()
        {
            var sponsor = new ClientSponsor();
            if (_listener != null)
            {
                Logger.LogLevelThreshold = _logLevel;
                Logger.RegisterListener(_listener);
                sponsor.Register(_listener);
            }
            try
            {
                try
                {
                    BuildDocument(_config, _manager, _baseDirectory, _outputDirectory, _pluginDirectory);
                }
                catch (AggregateException agg) when (agg.InnerException is DocfxException || agg.InnerException is DocumentException)
                {
                    throw new DocfxException(agg.InnerException.Message);
                }
                catch (DocfxException e)
                {
                    throw new DocfxException(e.Message);
                }
                catch (DocumentException e)
                {
                    throw new DocfxException(e.Message);
                }
                catch (Exception e)
                {
                    throw new DocfxException(e.ToString());
                }
            }
            finally
            {
                sponsor.Close();
            }
        }

        public static void BuildDocument(BuildJsonConfig config, TemplateManager templateManager, string baseDirectory, string outputDirectory, string pluginDirectory)
        {
            using (var builder = new DocumentBuilder(LoadPluginAssemblies(pluginDirectory)))
            {
                var parameters = ConfigToParameter(config, templateManager, baseDirectory, outputDirectory);
                if (parameters.Files.Count == 0)
                {
                    Logger.LogWarning("No files found, nothing is to be generated");
                    return;
                }

                using (new PerformanceScope("building documents", LogLevel.Info))
                {
                    builder.Build(parameters);
                }
            }
        }

        private static IEnumerable<Assembly> LoadPluginAssemblies(string pluginDirectory)
        {
            yield return typeof(ConceptualDocumentProcessor).Assembly;
            yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
            yield return typeof(ResourceDocumentProcessor).Assembly;
            yield return typeof(RestApiDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;

            if (pluginDirectory == null || !Directory.Exists(pluginDirectory))
            {
                yield break;
            }

            Logger.LogInfo($"Searching custom plugins in directory {pluginDirectory}...");

            foreach (var assemblyFile in Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
            {
                Assembly assembly = null;

                // assume assembly name is the same with file name without extension
                string assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    if (assemblyName == "Microsoft.DocAsCode.EntityModel")
                    {
                        // work around, don't load assembly Microsoft.DocAsCode.EntityModel.
                        Logger.LogWarning("Skipping assembly: Microsoft.DocAsCode.EntityModel.");
                        continue;
                    }
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                        Logger.LogVerbose($"Scanning assembly file {assemblyFile}...");
                    }
                    catch (Exception ex) when (ex is BadImageFormatException || ex is FileLoadException || ex is FileNotFoundException)
                    {
                        Logger.LogWarning($"Skipping file {assemblyFile} due to load failure: {ex.Message}");
                    }

                    if (assembly != null)
                    {
                        yield return assembly;
                    }
                }
            }
        }

        private static DocumentBuildParameters ConfigToParameter(BuildJsonConfig config, TemplateManager templateManager, string baseDirectory, string outputDirectory)
        {
            var parameters = new DocumentBuildParameters();
            parameters.OutputBaseDir = outputDirectory;
            if (config.GlobalMetadata != null)
            {
                parameters.Metadata = config.GlobalMetadata.ToImmutableDictionary();
            }
            if (config.FileMetadata != null)
            {
                parameters.FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata);
            }
            parameters.ExternalReferencePackages =
                GetFilesFromFileMapping(
                    GlobUtility.ExpandFileMapping(baseDirectory, config.ExternalReference))
                .ToImmutableArray();

            if (config.XRefMaps != null)
            {
                parameters.XRefMaps = config.XRefMaps.ToImmutableArray();
            }
            if (!config.NoLangKeyword)
            {
                parameters.XRefMaps = parameters.XRefMaps.Add("embedded:docfx/langwordMapping.yml");
            }

            parameters.Files = GetFileCollectionFromFileMapping(
                baseDirectory,
                GlobUtility.ExpandFileMapping(baseDirectory, config.Content),
                GlobUtility.ExpandFileMapping(baseDirectory, config.Overwrite),
                GlobUtility.ExpandFileMapping(baseDirectory, config.Resource));

            var applyTemplateSettings = new ApplyTemplateSettings(baseDirectory, outputDirectory)
            {
                TransformDocument = config.DryRun != true,
            };

            applyTemplateSettings.RawModelExportSettings.Export = config.ExportRawModel == true;
            if (!string.IsNullOrEmpty(config.RawModelOutputFolder))
            {
                applyTemplateSettings.RawModelExportSettings.OutputFolder = Path.Combine(baseDirectory, config.RawModelOutputFolder);
            }

            applyTemplateSettings.ViewModelExportSettings.Export = config.ExportViewModel == true;
            if (!string.IsNullOrEmpty(config.ViewModelOutputFolder))
            {
                applyTemplateSettings.ViewModelExportSettings.OutputFolder = Path.Combine(baseDirectory, config.ViewModelOutputFolder);
            }

            parameters.ApplyTemplateSettings = applyTemplateSettings;
            parameters.TemplateManager = templateManager;
            if (config.MaxParallelism == null || config.MaxParallelism.Value <= 0)
            {
                parameters.MaxParallelism = Environment.ProcessorCount;
            }
            else
            {
                parameters.MaxParallelism = config.MaxParallelism.Value;
            }
            if (config.MarkdownEngineName != null)
            {
                parameters.MarkdownEngineName = config.MarkdownEngineName;
            }
            if (config.MarkdownEngineProperties != null)
            {
                parameters.MarkdownEngineParameters = config.MarkdownEngineProperties.ToImmutableDictionary();
            }
            return parameters;
        }

        private static FileMetadata ConvertToFileMetadataItem(string baseDirectory, Dictionary<string, FileMetadataPairs> fileMetadata)
        {
            var result = new Dictionary<string, ImmutableArray<FileMetadataItem>>();
            foreach (var item in fileMetadata)
            {
                var list = new List<FileMetadataItem>();
                foreach (var pair in item.Value.Items)
                {
                    list.Add(new FileMetadataItem(pair.Glob, item.Key, pair.Value));
                }
                result.Add(item.Key, list.ToImmutableArray());
            }

            return new FileMetadata(baseDirectory, result);
        }

        private static IEnumerable<string> GetFilesFromFileMapping(FileMapping mapping)
        {
            if (mapping != null)
            {
                foreach (var file in mapping.Items)
                {
                    foreach (var item in file.Files)
                    {
                        yield return Path.Combine(file.SourceFolder ?? Environment.CurrentDirectory, item);
                    }
                }
            }
        }

        private static FileCollection GetFileCollectionFromFileMapping(
            string baseDirectory,
            FileMapping articles,
            FileMapping overwrites,
            FileMapping resources)
        {
            var fileCollection = new FileCollection(baseDirectory);
            AddFileMapping(fileCollection, baseDirectory, DocumentType.Article, articles);
            AddFileMapping(fileCollection, baseDirectory, DocumentType.Overwrite, overwrites);
            AddFileMapping(fileCollection, baseDirectory, DocumentType.Resource, resources);
            return fileCollection;
        }

        private static void AddFileMapping(FileCollection fileCollection, string baseDirectory, DocumentType type, FileMapping mapping)
        {
            if (mapping != null)
            {
                foreach (var item in mapping.Items)
                {
                    fileCollection.Add(
                        type,
                        item.Files,
                        s => RewritePath(baseDirectory, s, item));
                }
            }
        }

        private static string RewritePath(string baseDirectory, string sourcePath, FileMappingItem item)
        {
            return ConvertToDestinationPath(
                Path.Combine(baseDirectory, sourcePath),
                item.SourceFolder,
                item.DestinationFolder);
        }

        private static string ConvertToDestinationPath(string path, string src, string dest)
        {
            var relativePath = PathUtility.MakeRelativePath(src, path);
            return Path.Combine(dest ?? string.Empty, relativePath);
        }
    }
}
