// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class BuildCommand : ISubCommand
    {
        private readonly string _version;

        private readonly TemplateManager _templateManager;

        public BuildJsonConfig Config { get; }

        public bool AllowReplay => true;

        public BuildCommand(BuildCommandOptions options)
        {
            var assembly = typeof(Program).Assembly;
            _version = assembly.GetName().Version.ToString();
            Config = ParseOptions(options);
            SetDefaultConfigValue(Config);
            EnvironmentContext.BaseDirectory = Path.GetFullPath(string.IsNullOrEmpty(Config.BaseDirectory) ? Environment.CurrentDirectory : Config.BaseDirectory);
            _templateManager = new TemplateManager(assembly, "Template", Config.Templates, Config.Themes, Config.BaseDirectory);
        }

        public void Exec(SubCommandRunningContext context)
        {
            // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
            var baseDirectory = EnvironmentContext.BaseDirectory;
            var intermediateOutputFolder = Path.Combine(baseDirectory, "obj");
            var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(Config.OutputFolder) ? baseDirectory : Config.OutputFolder, Config.Destination ?? string.Empty));

            BuildDocument(baseDirectory, outputFolder);

            _templateManager.ProcessTheme(outputFolder, true);
            // TODO: SEARCH DATA

            if (Config?.Serve ?? false)
            {
                ServeCommand.Serve(outputFolder, Config.Port);
            }
        }

        #region BuildCommand ctor related

        private void SetDefaultConfigValue(BuildJsonConfig config)
        {
            if (Config.Templates == null || Config.Templates.Count == 0)
            {
                Config.Templates = new ListWithStringFallback { DocAsCode.Constants.DefaultTemplateName };
            }
            if (config.GlobalMetadata != null || !config.GlobalMetadata.ContainsKey("_docfxVersion"))
            {
                config.GlobalMetadata["_docfxVersion"] = _version;
            }
        }

        private static BuildJsonConfig ParseOptions(BuildCommandOptions options)
        {
            var configFile = options.ConfigFile;
            BuildJsonConfig config;
            if (string.IsNullOrEmpty(configFile))
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    if (options.Content == null && options.Resource == null)
                    {
                        throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                    }
                    else
                    {
                        config = new BuildJsonConfig();
                        MergeOptionsToConfig(options, config);
                        return config;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, $"Config file {Constants.ConfigFileName} is found.");
                    configFile = Constants.ConfigFileName;
                }
            }

            config = CommandUtility.GetConfig<BuildConfig>(configFile).Item;
            if (config == null) throw new DocumentException($"Unable to find build subcommand config in file '{configFile}'.");
            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, config);
            MergeGitContributeToConfig(config);
            return config;
        }

        private static void MergeOptionsToConfig(BuildCommandOptions options, BuildJsonConfig config)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Environment.CurrentDirectory;

            config.OutputFolder = options.OutputFolder;

            // Override config file with options from command line
            if (options.Templates != null && options.Templates.Count > 0)
            {
                config.Templates = new ListWithStringFallback(options.Templates);
            }

            if (options.PostProcessors != null && options.PostProcessors.Count > 0)
            {
                config.PostProcessors = new ListWithStringFallback(options.PostProcessors);
            }

            if (options.Themes != null && options.Themes.Count > 0)
            {
                config.Themes = new ListWithStringFallback(options.Themes);
            }
            if (!string.IsNullOrEmpty(options.OutputFolder))
            {
                config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
            }
            if (options.Content != null)
            {
                if (config.Content == null)
                {
                    config.Content = new FileMapping(new FileMappingItem());
                }
                config.Content.Add(
                    new FileMappingItem
                    {
                        Files = new FileItems(options.Content),
                        SourceFolder = optionsBaseDirectory,
                    });
            }
            if (options.Resource != null)
            {
                if (config.Resource == null)
                {
                    config.Resource = new FileMapping(new FileMappingItem());
                }
                config.Resource.Add(
                    new FileMappingItem
                    {
                        Files = new FileItems(options.Resource),
                        SourceFolder = optionsBaseDirectory,
                    });
            }
            if (options.Overwrite != null)
            {
                if (config.Overwrite == null)
                {
                    config.Overwrite = new FileMapping(new FileMappingItem());
                }
                config.Overwrite.Add(
                    new FileMappingItem
                    {
                        Files = new FileItems(options.Overwrite),
                        SourceFolder = optionsBaseDirectory,
                    });
            }
            if (options.ExternalReference != null)
            {
                if (config.ExternalReference == null)
                {
                    config.ExternalReference = new FileMapping(new FileMappingItem());
                }
                config.ExternalReference.Add(
                    new FileMappingItem
                    {
                        Files = new FileItems(options.ExternalReference),
                        SourceFolder = optionsBaseDirectory,
                    });
            }

            if (options.XRefMaps != null)
            {
                config.XRefMaps =
                    new ListWithStringFallback(
                        (config.XRefMaps ?? new ListWithStringFallback())
                        .Concat(options.XRefMaps)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());
            }

            if (options.Serve)
            {
                config.Serve = options.Serve;
            }
            if (options.Port.HasValue)
            {
                config.Port = options.Port.Value.ToString();
            }
            config.Force |= options.ForceRebuild;
            config.ExportRawModel |= options.ExportRawModel;
            config.ExportViewModel |= options.ExportViewModel;
            if (!string.IsNullOrEmpty(options.RawModelOutputFolder))
            {
                config.RawModelOutputFolder = Path.GetFullPath(options.RawModelOutputFolder);
            }
            if (!string.IsNullOrEmpty(options.ViewModelOutputFolder))
            {
                config.ViewModelOutputFolder = Path.GetFullPath(options.ViewModelOutputFolder);
            }
            config.DryRun |= options.DryRun;
            if (options.MaxParallelism != null)
            {
                config.MaxParallelism = options.MaxParallelism;
            }
            if (options.MarkdownEngineName != null)
            {
                config.MarkdownEngineName = options.MarkdownEngineName;
            }
            if (options.MarkdownEngineProperties != null)
            {
                config.MarkdownEngineProperties =
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        options.MarkdownEngineProperties,
                        new JsonSerializerSettings
                        {
                            Converters =
                            {
                                new JObjectDictionaryToObjectDictionaryConverter()
                            }
                        });
            }
            if (options.NoLangKeyword != null)
            {
                config.NoLangKeyword = options.NoLangKeyword.Value;
            }
            if (options.IntermediateFolder != null)
            {
                config.IntermediateFolder = options.IntermediateFolder;
            }
            if (options.GlobalMetadataFilePaths != null && !options.GlobalMetadataFilePaths.Any())
            {
                config.GlobalMetadataFilePaths.AddRange(options.GlobalMetadataFilePaths);
            }
            config.GlobalMetadataFilePaths =
                new ListWithStringFallback(config.GlobalMetadataFilePaths.Select(
                    path => PathUtility.IsRelativePath(path) ? Path.Combine(config.BaseDirectory, path) : path).Reverse());

            if (options.FileMetadataFilePaths != null && !options.FileMetadataFilePaths.Any())
            {
                config.FileMetadataFilePaths.AddRange(options.FileMetadataFilePaths);
            }
            config.FileMetadataFilePaths =
                new ListWithStringFallback(config.FileMetadataFilePaths.Select(
                    path => PathUtility.IsRelativePath(path) ? Path.Combine(config.BaseDirectory, path) : path).Reverse());

            config.FileMetadata = GetFileMetadataFromOption(config.FileMetadataFilePaths, config.FileMetadata);
            config.GlobalMetadata = GetGlobalMetadataFromOption(options.GlobalMetadata, config.GlobalMetadataFilePaths, config.GlobalMetadata);
        }

        private static void MergeGitContributeToConfig(BuildJsonConfig config)
        {
            GitDetail repoInfoFromBaseDirectory = GitUtility.GetGitDetail(Path.Combine(Environment.CurrentDirectory, config.BaseDirectory));

            if (repoInfoFromBaseDirectory?.RelativePath != null)
            {
                repoInfoFromBaseDirectory.RelativePath = Path.Combine(repoInfoFromBaseDirectory.RelativePath, DocAsCode.Constants.DefaultOverwriteFolderName);
            }
            object gitRespositoryOpenToPublicContributors;
            if (config.GlobalMetadata.TryGetValue("_gitContribute", out gitRespositoryOpenToPublicContributors))
            {
                GitDetail repoInfo;
                try
                {
                    repoInfo = JObject.FromObject(gitRespositoryOpenToPublicContributors).ToObject<GitDetail>();
                }
                catch (Exception e)
                {
                    throw new DocumentException($"Unable to convert _gitContribute to GitDetail in globalMetadata: {e.Message}", e);
                }
                if (repoInfoFromBaseDirectory != null)
                {
                    if (repoInfo.RelativePath == null) repoInfo.RelativePath = repoInfoFromBaseDirectory.RelativePath;
                    if (repoInfo.RemoteBranch == null) repoInfo.RemoteBranch = repoInfoFromBaseDirectory.RemoteBranch;
                    if (repoInfo.RemoteRepositoryUrl == null) repoInfo.RemoteRepositoryUrl = repoInfoFromBaseDirectory.RemoteRepositoryUrl;
                }
                config.GlobalMetadata["_gitContribute"] = repoInfo;
            }
            else
            {
                config.GlobalMetadata["_gitContribute"] = repoInfoFromBaseDirectory;
            }
        }

        internal static Dictionary<string, FileMetadataPairs> GetFileMetadataFromOption(ListWithStringFallback fileMetadataFilePaths, Dictionary<string, FileMetadataPairs> fileMetadataFromConfig)
        {
            var fileMetadataFromFile = new Dictionary<string, FileMetadataPairs>();
            if (fileMetadataFilePaths != null)
            {
                foreach (var fileMetadataFilePath in fileMetadataFilePaths)
                {
                    Dictionary<string, FileMetadataPairs> metadata = null;
                    try
                    {
                        metadata = JsonUtility.Deserialize<BuildJsonConfig>(fileMetadataFilePath).FileMetadata;
                    }
                    catch (FileNotFoundException)
                    {
                        Logger.LogWarning($"Invalid option \"{fileMetadataFilePath}\": file does not exist, ignored.");
                    }
                    catch (JsonException e)
                    {
                        Logger.LogWarning($"File from \"{fileMetadataFilePath}\" is not a valid JSON format file metadata, ignored: {e.Message}");
                    }
                    if (metadata == null)
                    {
                        Logger.LogWarning($"File from \"{fileMetadataFilePath}\" does not contain \"fileMetadata\" definition, ignored.");
                    }
                    else
                    {
                        fileMetadataFromFile = MergeDictionary(
                            new DictionaryMergeContext<FileMetadataPairs>($"fileMetdata from {fileMetadataFilePath}", metadata),
                            new DictionaryMergeContext<FileMetadataPairs>("fileMetdata from previous fileMetadataFile", fileMetadataFromFile));
                    }
                }
            }

            return MergeDictionary(
                new DictionaryMergeContext<FileMetadataPairs>("fileMetadata from docfx config file", fileMetadataFromConfig),
                new DictionaryMergeContext<FileMetadataPairs>("fileMetadata from fileMetadata config file", fileMetadataFromFile));
        }

        internal static Dictionary<string, object> GetGlobalMetadataFromOption(string globalMetadataContent, ListWithStringFallback globalMetadataFilePaths, Dictionary<string, object> globalMetadataFromConfig)
        {
            Dictionary<string, object> globalMetadata = null;
            if (globalMetadataContent != null)
            {
                using (var sr = new StringReader(globalMetadataContent))
                {
                    try
                    {
                        globalMetadata = JsonUtility.Deserialize<Dictionary<string, object>>(sr, GetToObjectSerializer());
                    }
                    catch (JsonException e)
                    {
                        Logger.LogWarning($"Metadata from \"--globalMetadata {globalMetadataContent}\" is not a valid JSON format global metadata, ignored: {e.Message}");
                    }
                }
            }

            if (globalMetadataFilePaths != null)
            {
                foreach (var globalMetadataFilePath in globalMetadataFilePaths)
                {
                    Dictionary<string, object> metadata = null;
                    try
                    {
                        metadata = JsonUtility.Deserialize<BuildJsonConfig>(globalMetadataFilePath).GlobalMetadata;

                    }
                    catch (FileNotFoundException)
                    {
                        Logger.LogWarning($"Invalid option \"globalMetadata config file {globalMetadataFilePath}\": file does not exist, ignored.");
                    }
                    catch (JsonException e)
                    {
                        Logger.LogWarning($"File from \"globalMetadata config file {globalMetadataFilePath}\" is not a valid JSON format global metadata, ignored: {e.Message}");
                    }
                    if (metadata == null)
                    {
                        Logger.LogWarning($" File from \"globalMetadata config file {globalMetadataFilePath}\" does not contain \"globalMetadata\" definition.");
                    }
                    else
                    {
                        globalMetadata = MergeDictionary(new DictionaryMergeContext<object>($"globalMetadata config file {globalMetadataFilePath}", metadata), new DictionaryMergeContext<object>("previous global metadata", globalMetadata));
                    }
                }
            }

            return MergeDictionary(
                new DictionaryMergeContext<object>("globalMetadata from docfx config file", globalMetadataFromConfig),
                new DictionaryMergeContext<object>("globalMetadata merged with command option and globalMetadata config file", globalMetadata)
                );
        }

        private static JsonSerializer GetToObjectSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            jsonSerializer.Converters.Add(new JObjectDictionaryToObjectDictionaryConverter());
            return jsonSerializer;
        }

        private static Dictionary<string, T> MergeDictionary<T>(DictionaryMergeContext<T> item, DictionaryMergeContext<T> overrideItem)
        {
            Dictionary<string, T> merged;
            if (overrideItem == null || overrideItem.Item == null)
            {
                merged = new Dictionary<string, T>();
            }
            else
            {
                merged = new Dictionary<string, T>(overrideItem.Item);
            }
            if (item == null || item.Item == null)
            {
                return merged;
            }
            else
            {
                foreach (var pair in item.Item)
                {
                    if (merged.ContainsKey(pair.Key))
                    {
                        Logger.LogWarning($"Both {item.Name} and {overrideItem.Name} contain definition for \"{pair.Key}\", the one from \"{overrideItem.Name}\" overrides the one from \"{item.Name}\".");
                    }
                    else
                    {
                        merged[pair.Key] = pair.Value;
                    }
                }
            }
            return merged;
        }

        private sealed class DictionaryMergeContext<T>
        {
            public string Name { get; }
            public Dictionary<string, T> Item { get; }

            public DictionaryMergeContext(string name, Dictionary<string, T> item)
            {
                Name = name;
                Item = item;
            }
        }

        private sealed class BuildConfig
        {
            [JsonProperty("build")]
            public BuildJsonConfig Item { get; set; }
        }

        #endregion

        #region Build document

        private void BuildDocument(string baseDirectory, string outputDirectory)
        {
            var pluginBaseFolder = AppDomain.CurrentDomain.BaseDirectory;
            var pluginFolderName = "plugins_" + Path.GetRandomFileName();
            var pluginFilePath = Path.Combine(pluginBaseFolder, pluginFolderName);
            var defaultPluginFolderPath = Path.Combine(pluginBaseFolder, "plugins");
            if (Directory.Exists(pluginFilePath))
            {
                throw new PluginDirectoryAlreadyExistsException(pluginFilePath);
            }

            bool created = false;
            try
            {
                created = _templateManager.TryExportTemplateFiles(pluginFilePath, @"^plugins/.*");
                if (created)
                {
                    BuildDocumentWithPlugin(Config, _templateManager, baseDirectory, outputDirectory, pluginBaseFolder, Path.Combine(pluginFilePath, "plugins"), pluginFilePath);
                }
                else
                {
                    if (Directory.Exists(defaultPluginFolderPath))
                    {
                        BuildDocumentWithPlugin(Config, _templateManager, baseDirectory, outputDirectory, pluginBaseFolder, defaultPluginFolderPath, null);
                    }
                    else
                    {
                        DocumentBuilderWrapper.BuildDocument(Config, _templateManager, baseDirectory, outputDirectory, null, null);
                    }
                }
            }
            finally
            {
                if (created)
                {
                    Logger.LogInfo($"Cleaning up temporary plugin folder \"{pluginFilePath}\"");
                }

                try
                {
                    if (Directory.Exists(pluginFilePath))
                    {
                        Directory.Delete(pluginFilePath, true);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Error occurs when cleaning up temporary plugin folder \"{pluginFilePath}\", please clean it up manually: {e.Message}");
                }
            }
        }

        private static void BuildDocumentWithPlugin(BuildJsonConfig config, TemplateManager manager, string baseDirectory, string outputDirectory, string applicationBaseDirectory, string pluginDirectory, string templateDirectory)
        {
            AppDomain builderDomain = null;
            try
            {
                var pluginConfig = Path.Combine(pluginDirectory, "docfx.plugins.config");
                Logger.LogInfo($"Plug-in directory: {pluginDirectory}, configuration file: {pluginConfig}");

                AppDomainSetup setup = new AppDomainSetup
                {
                    ApplicationBase = applicationBaseDirectory,
                    PrivateBinPath = string.Join(";", applicationBaseDirectory, pluginDirectory),
                    ConfigurationFile = pluginConfig
                };

                builderDomain = AppDomain.CreateDomain("document builder domain", null, setup);
                builderDomain.UnhandledException += (s, e) => { };
                builderDomain.DoCallBack(new DocumentBuilderWrapper(config, manager, baseDirectory, outputDirectory, pluginDirectory, new CrossAppDomainListener(), templateDirectory).BuildDocument);
            }
            finally
            {
                if (builderDomain != null)
                {
                    AppDomain.Unload(builderDomain);
                }
            }
        }

        #endregion
    }
}
