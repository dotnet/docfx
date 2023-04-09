﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Reflection;
using Microsoft.DocAsCode.Build.ConceptualDocuments;
using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Build.ManagedReference;
using Microsoft.DocAsCode.Build.ResourceFiles;
using Microsoft.DocAsCode.Build.RestApi;
using Microsoft.DocAsCode.Build.SchemaDriven;
using Microsoft.DocAsCode.Build.TableOfContents;
using Microsoft.DocAsCode.Build.UniversalReference;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

internal static class DocumentBuilderWrapper
{
    private static readonly Assembly[] s_pluginAssemblies = LoadPluginAssemblies(AppContext.BaseDirectory).ToArray();

    public static void BuildDocument(BuildJsonConfig config, BuildOptions options, TemplateManager templateManager, string baseDirectory, string outputDirectory, string pluginDirectory, string templateDirectory)
    {
        var postProcessorNames = config.PostProcessors.ToImmutableArray();
        var metadata = config.GlobalMetadata?.ToImmutableDictionary();

        // For backward compatible, retain "_enableSearch" to globalMetadata though it's deprecated
        if (metadata != null && metadata.TryGetValue("_enableSearch", out object value))
        {
            if (value is bool isSearchable && isSearchable && !postProcessorNames.Contains("ExtractSearchIndex"))
            {
                postProcessorNames = postProcessorNames.Add("ExtractSearchIndex");
            }
        }

        if (!string.IsNullOrEmpty(config.SitemapOptions?.BaseUrl))
        {
            postProcessorNames = postProcessorNames.Add("SitemapGenerator");
        }

        using var builder = new DocumentBuilder(s_pluginAssemblies, postProcessorNames);
        using (new PerformanceScope("building documents", LogLevel.Info))
        {
            var parameters = ConfigToParameter(config, options, templateManager, baseDirectory, outputDirectory, templateDirectory);
            builder.Build(parameters, outputDirectory);
        }
    }

    private static IEnumerable<Assembly> LoadPluginAssemblies(string pluginDirectory)
    {
        var defaultPluggedAssemblies = new List<Assembly>
        {
            typeof(ConceptualDocumentProcessor).Assembly,
            typeof(ManagedReferenceDocumentProcessor).Assembly,
            typeof(ResourceDocumentProcessor).Assembly,
            typeof(RestApiDocumentProcessor).Assembly,
            typeof(TocDocumentProcessor).Assembly,
            typeof(SchemaDrivenDocumentProcessor).Assembly,
            typeof(UniversalReferenceDocumentProcessor).Assembly,
        };
        foreach (var assem in defaultPluggedAssemblies)
        {
            yield return assem;
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
                    Logger.LogVerbose("Skipping assembly: Microsoft.DocAsCode.EntityModel.");
                    continue;
                }
                if (assemblyName == typeof(ValidateBookmark).Assembly.GetName().Name)
                {
                    // work around, don't load assembly that has ValidateBookmark, to prevent double loading
                    Logger.LogVerbose($"Skipping assembly: {assemblyName}.");
                    continue;
                }

                if (defaultPluggedAssemblies.Select(n => n.GetName().Name).Contains(assemblyName))
                {
                    Logger.LogVerbose($"Skipping default plugged assembly: {assemblyName}.");
                    continue;
                }

                try
                {
                    assembly = Assembly.Load(assemblyName);

                    Logger.LogVerbose($"Scanning assembly file {assemblyFile}...");

                    // Verify assembly is loadable
                    assembly.DefinedTypes.Select(type => type.AsType()).Count();
                }
                catch (Exception ex)
                {
                    Logger.LogVerbose($"Skipping file {assemblyFile} due to load failure: {ex.Message}");
                    continue;
                }

                if (assembly != null)
                {
                    yield return assembly;
                }
            }
        }
    }

    private static List<DocumentBuildParameters> ConfigToParameter(BuildJsonConfig config, BuildOptions options, TemplateManager templateManager, string baseDirectory, string outputDirectory, string templateDir)
    {
        using (new PerformanceScope("GenerateParameters"))
        {
            var result = new List<DocumentBuildParameters>();

            var parameters = new DocumentBuildParameters
            {
                OutputBaseDir = outputDirectory,
                SitemapOptions = config.SitemapOptions,
                FALName = config.FALName,
                DisableGitFeatures = config.DisableGitFeatures,
                TagParameters = config.TagParameters,
                ConfigureMarkdig = options.ConfigureMarkdig,
            };
            if (config.GlobalMetadata != null)
            {
                parameters.Metadata = config.GlobalMetadata.ToImmutableDictionary();
            }
            if (config.FileMetadata != null)
            {
                parameters.FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata);
            }
            if (config.PostProcessors != null)
            {
                parameters.PostProcessors = config.PostProcessors.ToImmutableArray();
            }
            if (config.XRefMaps != null)
            {
                parameters.XRefMaps = config.XRefMaps.ToImmutableArray();
            }

            string outputFolderForDebugFiles = null;
            if (!string.IsNullOrEmpty(config.OutputFolderForDebugFiles))
            {
                outputFolderForDebugFiles = Path.Combine(baseDirectory, config.OutputFolderForDebugFiles);
            }

            var applyTemplateSettings = new ApplyTemplateSettings(baseDirectory, outputDirectory, outputFolderForDebugFiles, config.EnableDebugMode ?? false)
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
                ThreadPool.GetMinThreads(out int wt, out int cpt);
                if (wt < parameters.MaxParallelism)
                {
                    ThreadPool.SetMinThreads(parameters.MaxParallelism, cpt);
                }
            }

            parameters.MaxHttpParallelism = Math.Max(64, parameters.MaxParallelism * 2);
            ServicePointManager.DefaultConnectionLimit = parameters.MaxHttpParallelism;

            if (config.MarkdownEngineProperties != null)
            {
                parameters.MarkdownEngineParameters = config.MarkdownEngineProperties.ToImmutableDictionary();
            }
            if (config.CustomLinkResolver != null)
            {
                parameters.CustomLinkResolver = config.CustomLinkResolver;
            }

            parameters.TemplateDir = templateDir;

            var fileMappingParametersDictionary = GroupFileMappings(config.Content, config.Overwrite, config.Resource);

            if (config.LruSize == null)
            {
                parameters.LruSize = Environment.Is64BitProcess ? 0x2000 : 0xC00;
            }
            else
            {
                parameters.LruSize = Math.Max(0, config.LruSize.Value);
            }

            if (config.KeepFileLink)
            {
                parameters.KeepFileLink = true;
            }

            if (config.Pairing != null)
            {
                parameters.OverwriteFragmentsRedirectionRules = config.Pairing.Select(i => new FolderRedirectionRule(i.ContentFolder, i.OverwriteFragmentsFolder)).ToImmutableArray();
            }

            parameters.XRefTags = config.XrefTags;

            foreach (var pair in fileMappingParametersDictionary)
            {
                var p = parameters.Clone();
                if (!string.IsNullOrEmpty(pair.Key))
                {
                    p.GroupInfo = new GroupInfo
                    {
                        Name = pair.Key,
                    };
                    if (config.Groups != null && config.Groups.TryGetValue(pair.Key, out GroupConfig gi))
                    {
                        p.GroupInfo.Destination = gi.Destination;
                        p.GroupInfo.XRefTags = gi.XrefTags ?? new List<string>();
                        p.GroupInfo.Metadata = gi.Metadata;
                        if (!string.IsNullOrEmpty(gi.Destination))
                        {
                            p.VersionDir = gi.Destination;
                        }
                    }
                }
                p.Files = GetFileCollectionFromFileMapping(
                    baseDirectory,
                    GlobUtility.ExpandFileMapping(baseDirectory, pair.Value.GetFileMapping(FileMappingType.Content)),
                    GlobUtility.ExpandFileMapping(baseDirectory, pair.Value.GetFileMapping(FileMappingType.Overwrite)),
                    GlobUtility.ExpandFileMapping(baseDirectory, pair.Value.GetFileMapping(FileMappingType.Resource)));
                p.VersionName = pair.Key;
                p.RootTocPath = pair.Value.RootTocPath;
                result.Add(p);
            }

            return result;
        }
    }

    /// <summary>
    /// Group FileMappings to a dictionary using VersionName as the key.
    /// As default version has no VersionName, using empty string as the key.
    /// </summary>
    private static Dictionary<string, FileMappingParameters> GroupFileMappings(FileMapping content,
        FileMapping overwrite, FileMapping resource)
    {
        var result = new Dictionary<string, FileMappingParameters>
        {
            [string.Empty] = new FileMappingParameters()
        };

        AddFileMappingTypeGroup(result, content, FileMappingType.Content);
        AddFileMappingTypeGroup(result, overwrite, FileMappingType.Overwrite);
        AddFileMappingTypeGroup(result, resource, FileMappingType.Resource);

        return result;
    }

    private static void AddFileMappingTypeGroup(
        Dictionary<string, FileMappingParameters> fileMappingsDictionary,
        FileMapping fileMapping,
        FileMappingType type)
    {
        if (fileMapping == null) return;
        foreach (var item in fileMapping.Items)
        {
            var version = item.GroupName ?? item.VersionName ?? string.Empty;
            if (fileMappingsDictionary.TryGetValue(version, out FileMappingParameters parameters))
            {
                if (parameters.TryGetValue(type, out FileMapping mapping))
                {
                    mapping.Add(item);
                }
                else
                {
                    parameters[type] = new FileMapping(item);
                }
            }
            else
            {
                fileMappingsDictionary[version] = new FileMappingParameters
                {
                    [type] = new FileMapping(item)
                };
            }
        }
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
                    yield return Path.Combine(file.SourceFolder ?? Directory.GetCurrentDirectory(), item);
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
        AddFileMapping(fileCollection, DocumentType.Article, articles);
        AddFileMapping(fileCollection, DocumentType.Overwrite, overwrites);
        AddFileMapping(fileCollection, DocumentType.Resource, resources);
        return fileCollection;
    }

    private static void AddFileMapping(FileCollection fileCollection, DocumentType type, FileMapping mapping)
    {
        if (mapping != null)
        {
            foreach (var item in mapping.Items)
            {
                fileCollection.Add(
                    type,
                    item.Files,
                    item.SourceFolder,
                    item.DestinationFolder);
            }
        }
    }

    private class FileMappingParameters : Dictionary<FileMappingType, FileMapping>
    {
        public FileMapping GetFileMapping(FileMappingType type)
        {
            TryGetValue(type, out FileMapping result);
            return result;
        }

        public string RootTocPath
        {
            get
            {
                var mapping = GetFileMapping(FileMappingType.Content);
                return mapping?.RootTocPath;
            }
        }
    }

    private enum FileMappingType
    {
        Content,
        Overwrite,
        Resource,
    }
}
