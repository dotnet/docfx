// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;

namespace Docfx;

internal static class DocumentBuilderWrapper
{
    private static readonly Assembly[] s_pluginAssemblies = LoadPluginAssemblies(AppContext.BaseDirectory).ToArray();

    public static void BuildDocument(BuildJsonConfig config, BuildOptions options, TemplateManager templateManager, string baseDirectory, string outputDirectory, string templateDirectory, CancellationToken cancellationToken)
    {
        var postProcessorNames = config.PostProcessors.ToImmutableArray();
        var metadata = config.GlobalMetadata?.ToImmutableDictionary();

        // Ensure "_enableSearch" adds the right post processor
        if (metadata != null && metadata.TryGetValue("_enableSearch", out object value))
        {
            if (value is true && !postProcessorNames.Contains("ExtractSearchIndex"))
            {
                postProcessorNames = postProcessorNames.Add("ExtractSearchIndex");
            }
        }

        if (!string.IsNullOrEmpty(config.Sitemap?.BaseUrl))
        {
            postProcessorNames = postProcessorNames.Add("SitemapGenerator");
        }

        var pluginAssemblies = templateManager.GetTemplateDirectories().Select(d => Path.Combine(d, "plugins")).SelectMany(LoadPluginAssemblies);

        using var builder = new DocumentBuilder(s_pluginAssemblies.Concat(pluginAssemblies), postProcessorNames);

        var parameters = ConfigToParameter(config, options, templateManager, baseDirectory, outputDirectory, templateDirectory);
        builder.Build(parameters, outputDirectory, cancellationToken);
    }

    private static IEnumerable<Assembly> LoadPluginAssemblies(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
            yield break;

        if (pluginDirectory == AppContext.BaseDirectory)
            Logger.LogInfo($"Searching built-in plugins in directory {pluginDirectory}...");
        else
            Logger.LogInfo($"Searching custom plugins in directory {pluginDirectory}...");

        foreach (string assemblyFile in Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            // assume assembly name is the same with file name without extension
            string assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
            if (!string.IsNullOrEmpty(assemblyName))
            {
                if (assemblyName == "Docfx.EntityModel" || assemblyName.StartsWith("System."))
                {
                    // work around, don't load assembly Docfx.EntityModel.
                    Logger.LogVerbose($"Skipping assembly: {assemblyName}");
                    continue;
                }
                if (assemblyName == typeof(ValidateBookmark).Assembly.GetName().Name)
                {
                    // work around, don't load assembly that has ValidateBookmark, to prevent double loading
                    Logger.LogVerbose($"Skipping assembly: {assemblyName}.");
                    continue;
                }

                if (!IsDocfxPluginAssembly(assemblyFile))
                {
                    Logger.LogVerbose($"Skipping non-plugin assembly: {assemblyName}.");
                    continue;
                }

                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyFile);

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

        static bool IsDocfxPluginAssembly(string assemblyFile)
        {
            try
            {
                // Determines if the input assembly file is potentially a docfx plugin assembly
                // by checking if referenced assemblies contains Docfx.Plugins using MetadataReader
                using (var stream = File.OpenRead(assemblyFile))
                using (var peReader = new PEReader(stream))
                {
                    var metadataReader = peReader.GetMetadataReader();
                    return metadataReader.AssemblyReferences.Any(a => metadataReader.GetString(metadataReader.GetAssemblyReference(a).Name) == "Docfx.Plugins");
                }
            }
            catch (Exception ex)
            {
                Logger.LogVerbose($"Skipping file {assemblyFile} due to load failure: {ex.Message}");
                return false;
            }
        }
    }

    private static List<DocumentBuildParameters> ConfigToParameter(BuildJsonConfig config, BuildOptions options, TemplateManager templateManager, string baseDirectory, string outputDirectory, string templateDir)
    {
        var result = new List<DocumentBuildParameters>();

        var parameters = new DocumentBuildParameters
        {
            OutputBaseDir = outputDirectory,
            SitemapOptions = config.Sitemap,
            DisableGitFeatures = config.DisableGitFeatures,
            ConfigureMarkdig = options.ConfigureMarkdig,
            Metadata = GetGlobalMetadata(config),
            FileMetadata = GetFileMetadata(baseDirectory, config),
        };

        if (config.PostProcessors != null)
        {
            parameters.PostProcessors = config.PostProcessors.ToImmutableArray();
        }
        if (config.Xref != null)
        {
            parameters.XRefMaps = config.Xref.ToImmutableArray();
        }

        string outputFolderForDebugFiles = null;
        if (!string.IsNullOrEmpty(config.DebugOutput))
        {
            outputFolderForDebugFiles = Path.Combine(baseDirectory, config.DebugOutput);
        }

        var applyTemplateSettings = new ApplyTemplateSettings(baseDirectory, outputDirectory, outputFolderForDebugFiles, config.Debug ?? false)
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

        if (config.MarkdownEngineProperties != null)
        {
            parameters.MarkdownEngineParameters = config.MarkdownEngineProperties;
        }
        if (config.CustomLinkResolver != null)
        {
            parameters.CustomLinkResolver = config.CustomLinkResolver;
        }

        parameters.TemplateDir = templateDir;

        var fileMappingParametersDictionary = GroupFileMappings(config.Content, config.Overwrite, config.Resource);

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

    private static ImmutableDictionary<string, object> GetGlobalMetadata(BuildJsonConfig config)
    {
        var result = new Dictionary<string, object>();

        if (config.GlobalMetadata != null)
        {
            foreach (var (key, value) in config.GlobalMetadata)
            {
                result[key] = value;
            }
        }

        if (config.GlobalMetadataFiles != null)
        {
            foreach (var path in config.GlobalMetadataFiles)
            {
                foreach (var (key, value) in JsonUtility.Deserialize<Dictionary<string, object>>(path))
                {
                    result[key] = value;
                }
            }
        }

        return result.ToImmutableDictionary();
    }

    private static FileMetadata GetFileMetadata(string baseDirectory, BuildJsonConfig config)
    {
        var result = new Dictionary<string, List<FileMetadataItem>>();

        if (config.FileMetadata != null)
        {
            foreach (var (key, value) in config.FileMetadata)
            {
                var list = result.TryGetValue(key, out var items) ? items : result[key] = [];
                foreach (var pair in value.Items)
                {
                    list.Add(new FileMetadataItem(pair.Glob, key, pair.Value));
                }
            }
        }

        if (config.FileMetadataFiles != null)
        {
            foreach (var path in config.FileMetadataFiles)
            {
                foreach (var (key, value) in JsonUtility.Deserialize<Dictionary<string, FileMetadataPairs>>(path))
                {
                    var list = result.TryGetValue(key, out var items) ? items : result[key] = [];
                    foreach (var pair in value.Items)
                    {
                        list.Add(new FileMetadataItem(pair.Glob, key, pair.Value));
                    }
                }
            }
        }

        return new FileMetadata(baseDirectory, result.ToDictionary(p => p.Key, p => p.Value.ToImmutableArray()));
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
            string version = item.Group ?? string.Empty;
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
                    item.Src,
                    item.Dest);
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
