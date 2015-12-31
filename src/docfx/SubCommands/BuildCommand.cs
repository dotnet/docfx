// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    internal sealed class BuildCommand : ISubCommand
    {
        public BuildJsonConfig Config { get; }

        public BuildCommand(BuildCommandOptions options)
        {
            Config = ParseOptions(options);
        }

        public void Exec(SubCommandRunningContext context)
        {
            var config = Config;
            var parameters = ConfigToParameter(config);
            if (parameters.Files.Count == 0)
            {
                Logger.LogWarning("No files found, nothing is to be generated");
                return;
            }

            BuildDocument(config);

            var documentContext = DocumentBuildContext.DeserializeFrom(parameters.OutputBaseDir);
            var assembly = typeof(Program).Assembly;

            if (config.Templates == null || config.Templates.Count == 0)
            {
                config.Templates = new ListWithStringFallback { DocAsCode.Constants.DefaultTemplateName };
            }

            // If RootOutput folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(config.OutputFolder ?? config.BaseDirectory ?? string.Empty, config.Destination ?? string.Empty);
            using (var manager = new TemplateManager(assembly, "Template", config.Templates, config.Themes, config.BaseDirectory))
            {
                manager.ProcessTemplateAndTheme(documentContext, outputFolder, true);
            }

            // TODO: SEARCH DATA

            if (config?.Serve ?? false)
            {
                ServeCommand.Serve(outputFolder, config.Port);
            }
        }

        private static DocumentBuildParameters ConfigToParameter(BuildJsonConfig config)
        {
            var parameters = new DocumentBuildParameters();
            var baseDirectory = config.BaseDirectory ?? Environment.CurrentDirectory;

            parameters.OutputBaseDir = Path.Combine(baseDirectory, "obj");
            if (config.GlobalMetadata != null) parameters.Metadata = config.GlobalMetadata.ToImmutableDictionary();
            if (config.FileMetadata != null) parameters.FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata);
            parameters.ExternalReferencePackages = GetFilesFromFileMapping(GlobUtility.ExpandFileMapping(baseDirectory, config.ExternalReference)).ToImmutableArray();
            parameters.Files = GetFileCollectionFromFileMapping(
                baseDirectory,
                Tuple.Create(DocumentType.Article, GlobUtility.ExpandFileMapping(baseDirectory, config.Content)),
                Tuple.Create(DocumentType.Override, GlobUtility.ExpandFileMapping(baseDirectory, config.Overwrite)),
                Tuple.Create(DocumentType.Resource, GlobUtility.ExpandFileMapping(baseDirectory, config.Resource)));
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
            if (mapping == null) yield break;
            foreach (var file in mapping.Items)
            {
                foreach (var item in file.Files)
                {
                    yield return Path.Combine(file.SourceFolder ?? Environment.CurrentDirectory, item);
                }
            }
        }

        private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, params Tuple<DocumentType, FileMapping>[] files)
        {
            var fileCollection = new FileCollection(baseDirectory);
            foreach (var file in files)
            {
                if (file.Item2 != null)
                {
                    foreach (var mapping in file.Item2.Items)
                    {
                        fileCollection.Add(file.Item1, mapping.Files, s => ConvertToDestinationPath(Path.Combine(baseDirectory, s), mapping.SourceFolder, mapping.DestinationFolder));
                    }
                }
            }

            return fileCollection;
        }

        private static string ConvertToDestinationPath(string path, string src, string dest)
        {
            var relativePath = PathUtility.MakeRelativePath(src, path);
            return Path.Combine(dest ?? string.Empty, relativePath);
        }

        private static void BuildDocument(BuildJsonConfig config)
        {
            AppDomain builderDomain = null;
            try
            {
                string applicationBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
                string pluginConfig = Path.Combine(pluginDirectory, "docfx.plugins.config");

                Logger.LogInfo($"Plug-in directory: {pluginDirectory}, configuration file: {pluginConfig}");

                AppDomainSetup setup = new AppDomainSetup
                {
                    ApplicationBase = applicationBaseDirectory,
                    PrivateBinPath = string.Join(";", applicationBaseDirectory, pluginDirectory),
                    ConfigurationFile = pluginConfig
                };

                builderDomain = AppDomain.CreateDomain("document builder domain", null, setup);

                // TODO: redirect logged items into current domain
                builderDomain.DoCallBack(new DocumentBuilderWrapper(pluginDirectory, config).BuildDocument);
            }
            catch (DocumentException ex)
            {
                Logger.LogWarning("document error:" + ex.Message);
            }
            finally
            {
                if (builderDomain != null)
                {
                    AppDomain.Unload(builderDomain);
                }
            }
        }
        private static BuildJsonConfig ParseOptions(BuildCommandOptions options)
        {
            var configFile = options.ConfigFile;
            BuildJsonConfig config;
            if (string.IsNullOrEmpty(configFile))
            {
                if (!File.Exists(DocAsCode.Constants.ConfigFileName))
                {
                    if (options.Content == null && options.Resource == null)
                    {
                        throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                    }
                    else
                    {
                        config = new BuildJsonConfig();
                        MergeOptionsToConfig(options, ref config);
                        return config;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, $"Config file {DocAsCode.Constants.ConfigFileName} is found.");
                    configFile = DocAsCode.Constants.ConfigFileName;
                }
            }

            config = CommandUtility.GetConfig<BuildConfig>(configFile).Item;
            if (config == null) throw new DocumentException($"Unable to find build subcommand config in file '{configFile}'.");
            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, ref config);

            return config;
        }

        private static void MergeOptionsToConfig(BuildCommandOptions options, ref BuildJsonConfig config)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Environment.CurrentDirectory;

            config.OutputFolder = options.OutputFolder;

            // Override config file with options from command line
            if (options.Templates != null && options.Templates.Count > 0) config.Templates = new ListWithStringFallback(options.Templates);

            if (options.Themes != null && options.Themes.Count > 0) config.Themes = new ListWithStringFallback(options.Themes);
            if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
            if (options.Content != null)
            {
                if (config.Content == null) config.Content = new FileMapping(new FileMappingItem());
                config.Content.Add(new FileMappingItem() { Files = new FileItems(options.Content), SourceFolder = optionsBaseDirectory });
            }
            if (options.Resource != null)
            {
                if (config.Resource == null) config.Resource = new FileMapping(new FileMappingItem());
                config.Resource.Add(new FileMappingItem() { Files = new FileItems(options.Resource), SourceFolder = optionsBaseDirectory });
            }
            if (options.Overwrite != null)
            {
                if (config.Overwrite == null) config.Overwrite = new FileMapping(new FileMappingItem());
                config.Overwrite.Add(new FileMappingItem() { Files = new FileItems(options.Overwrite), SourceFolder = optionsBaseDirectory });
            }
            if (options.ExternalReference != null)
            {
                if (config.ExternalReference == null) config.ExternalReference = new FileMapping(new FileMappingItem());
                config.ExternalReference.Add(new FileMappingItem() { Files = new FileItems(options.ExternalReference), SourceFolder = optionsBaseDirectory });
            }
            if (options.Serve) config.Serve = options.Serve;
            if (options.Port.HasValue) config.Port = options.Port.Value.ToString();
            config.Force |= options.ForceRebuild;
        }

        [Serializable]
        private sealed class DocumentBuilderWrapper
        {
            private readonly string _pluginDirectory;
            private readonly BuildJsonConfig _config;

            public DocumentBuilderWrapper(string pluginDirectory, BuildJsonConfig config)
            {
                if (string.IsNullOrEmpty(pluginDirectory))
                {
                    throw new ArgumentNullException(nameof(_pluginDirectory));
                }

                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                _pluginDirectory = pluginDirectory;
                _config = config;
            }

            public void BuildDocument()
            {
                var builder = new DocumentBuilder(LoadPluginAssemblies(_pluginDirectory));

                var parameters = ConfigToParameter(_config);
                if (parameters.Files.Count == 0)
                {
                    Logger.LogWarning("No files found, nothing is to be generated");
                    return;
                }

                builder.Build(parameters);
            }

            private static IEnumerable<Assembly> LoadPluginAssemblies(string pluginDirectory)
            {
                if (!Directory.Exists(pluginDirectory))
                {
                    yield break;
                }

                Logger.LogInfo($"Searching custom plug-ins in directory {pluginDirectory}...");

                foreach (var assemblyFile in Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
                {
                    Assembly assembly = null;

                    // assume assembly name is the same with file name without extension
                    string assemblyName = Path.GetFileNameWithoutExtension(assemblyFile);
                    if (!string.IsNullOrEmpty(assemblyName))
                    {
                        try
                        {
                            assembly = Assembly.Load(assemblyName);
                            Logger.LogInfo($"Scanning assembly file {assemblyFile}...");
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
        }

        private sealed class BuildConfig
        {
            [JsonProperty("build")]
            public BuildJsonConfig Item { get; set; }
        }
    }
}
