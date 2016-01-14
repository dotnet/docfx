// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Loggers;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;

    internal sealed class BuildCommand : ISubCommand
    {
        private static readonly ThreadLocal<JsonSerializer> _toObjectSerializer = new ThreadLocal<JsonSerializer>(
            () =>
            {
                var jsonSerializer = new JsonSerializer();
                jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
                jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
                jsonSerializer.Converters.Add(new JObjectDictionaryToObjectDictionaryConverter());
                return jsonSerializer;
            });

        private TemplateManager _templateManager;

        public BuildJsonConfig Config { get; }

        public BuildCommand(BuildCommandOptions options)
        {
            Config = ParseOptions(options);
            if (Config.Templates == null || Config.Templates.Count == 0)
            {
                Config.Templates = new ListWithStringFallback { DocAsCode.Constants.DefaultTemplateName };
            }

            var assembly = typeof(Program).Assembly;
            _templateManager = new TemplateManager(assembly, "Template", Config.Templates, Config.Themes, Config.BaseDirectory);
        }

        public void Exec(SubCommandRunningContext context)
        {
            var config = Config;
            var baseDirectory = config.BaseDirectory ?? Environment.CurrentDirectory;
            var intermediateOutputFolder = Path.Combine(baseDirectory, "obj");

            BuildDocument(baseDirectory, intermediateOutputFolder);

            var documentContext = DocumentBuildContext.DeserializeFrom(intermediateOutputFolder);

            // If RootOutput folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(config.OutputFolder ?? config.BaseDirectory ?? string.Empty, config.Destination ?? string.Empty);
            _templateManager.ProcessTemplateAndTheme(documentContext, outputFolder, true);

            // TODO: SEARCH DATA

            if (config?.Serve ?? false)
            {
                ServeCommand.Serve(outputFolder, config.Port);
            }
        }

        #region BuildCommand ctor related
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
            var fileMetadata = GetFileMetadataFromOption(options);
            if (fileMetadata != null) config.FileMetadata = fileMetadata;
            var globalMetadata = GetGlobalMetadataFromOption(options);
            if (globalMetadata != null) config.GlobalMetadata = globalMetadata;
        }

        private static Dictionary<string, FileMetadataPairs> GetFileMetadataFromOption(BuildCommandOptions options)
        {
            if (options.FileMetadataFilePath != null)
            {
                try
                {
                    var fileMetadata = JsonUtility.Deserialize<BuildJsonConfig>(options.FileMetadataFilePath).FileMetadata;
                    if (fileMetadata == null)
                    {
                        Logger.LogWarning($"File from \"--fileMetadataFile {options.FileMetadataFilePath}\" does not contain \"fileMetadata\" definition, ignored.");
                    }
                    else
                    {
                        Logger.LogInfo($"File metadata from \"--fileMetadataFile {options.FileMetadataFilePath}\" overrides the one defined in config file");
                    }
                }
                catch (FileNotFoundException)
                {
                    Logger.LogWarning($"Invalid option \"--fileMetadataFile {options.FileMetadataFilePath}\": file does not exist, ignored.");
                }
                catch (JsonException e)
                {
                    Logger.LogWarning($"File from \"--fileMetadataFile {options.FileMetadataFilePath}\" is not a valid JSON format file metadata, ignored: {e.Message}");
                }
            }
            return null;
        }

        private static Dictionary<string, object> GetGlobalMetadataFromOption(BuildCommandOptions options)
        {
            Dictionary<string, object> globalMetadata = null;
            if (options.GlobalMetadata != null)
            {
                using (var sr = new StringReader(options.GlobalMetadata))
                {
                    try
                    {
                        globalMetadata = JsonUtility.Deserialize<Dictionary<string, object>>(sr, _toObjectSerializer.Value);
                        if (globalMetadata != null && globalMetadata.Count > 0)
                        {
                            Logger.LogInfo($"Global metadata from \"--globalMetadata\" overrides the one defined in config file");
                        }
                    }
                    catch (JsonException e)
                    {
                        Logger.LogWarning($"Metadata from \"--globalMetadata {options.GlobalMetadata}\" is not a valid JSON format global metadata, ignored: {e.Message}");
                    }
                }
            }

            if (options.GlobalMetadataFilePath != null)
            {
                try
                {
                    var globalMetadataFromFile = JsonUtility.Deserialize<BuildJsonConfig>(options.GlobalMetadataFilePath).GlobalMetadata;
                    if (globalMetadataFromFile == null)
                    {
                        Logger.LogWarning($" File from \"--globalMetadataFile {options.GlobalMetadataFilePath}\" does not contain \"globalMetadata\" definition.");
                    }
                    else
                    {
                        if (globalMetadata == null) globalMetadata = globalMetadataFromFile;
                        else
                        {
                            foreach (var pair in globalMetadataFromFile)
                            {
                                if (globalMetadata.ContainsKey(pair.Key))
                                {
                                    Logger.LogWarning($"Both --globalMetadata and --globalMetadataFile contain definition for \"{pair.Key}\", the one from \"--globalMetadata\" overrides the one from \"--globalMetadataFile {options.GlobalMetadataFilePath}\".");
                                }
                                else
                                {
                                    globalMetadata[pair.Key] = pair.Value;
                                }
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    Logger.LogWarning($"Invalid option \"--globalMetadataFile {options.GlobalMetadataFilePath}\": file does not exist, ignored.");
                }
                catch (JsonException e)
                {
                    Logger.LogWarning($"File from \"--globalMetadataFile {options.GlobalMetadataFilePath}\" is not a valid JSON format global metadata, ignored: {e.Message}");
                }
            }

            if (globalMetadata?.Count > 0)
            {
                return globalMetadata;
            }
            return null;
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
                    BuildDocumentWithPlugin(Config, baseDirectory, outputDirectory, pluginBaseFolder, Path.Combine(pluginFilePath, "plugins"));
                }
                else
                {
                    DocumentBuilderWrapper.BuildDocument(Config, baseDirectory, outputDirectory, null);
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

        private static void BuildDocumentWithPlugin(BuildJsonConfig config, string baseDirectory, string outputDirectory, string applicationBaseDirectory, string pluginDirectory)
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
                builderDomain.DoCallBack(new DocumentBuilderWrapper(config, baseDirectory, outputDirectory, pluginDirectory, new CrossAppDomainListener()).BuildDocument);
            }
            finally
            {
                if (builderDomain != null)
                {
                    AppDomain.Unload(builderDomain);
                }
            }
        }

        [Serializable]
        private sealed class DocumentBuilderWrapper
        {
            private readonly string _pluginDirectory;
            private readonly string _baseDirectory;
            private readonly string _outputDirectory;
            private readonly BuildJsonConfig _config;
            private readonly ILoggerListener _listener;

            public DocumentBuilderWrapper(BuildJsonConfig config, string baseDirectory, string outputDirectory, string pluginDirectory, ILoggerListener listener)
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
            }

            public void BuildDocument()
            {
                if (_listener != null)
                {
                    Logger.RegisterListener(_listener);
                }

                BuildDocument(_config, _baseDirectory, _outputDirectory, _pluginDirectory);
            }

            public static void BuildDocument(BuildJsonConfig config, string baseDirectory, string outputDirectory, string pluginDirectory)
            {
                try
                {
                    var builder = new DocumentBuilder(LoadPluginAssemblies(pluginDirectory));

                    var parameters = ConfigToParameter(config, baseDirectory, outputDirectory);
                    if (parameters.Files.Count == 0)
                    {
                        Logger.LogWarning("No files found, nothing is to be generated");
                        return;
                    }

                    builder.Build(parameters);
                }
                catch (DocumentException ex)
                {
                    Logger.LogWarning("Document error occurs:" + ex.Message);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                }
            }

            private static IEnumerable<Assembly> LoadPluginAssemblies(string pluginDirectory)
            {
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

            private static DocumentBuildParameters ConfigToParameter(BuildJsonConfig config, string baseDirectory, string outputDirectory)
            {
                var parameters = new DocumentBuildParameters();

                parameters.OutputBaseDir = outputDirectory;
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
        }

        #endregion
    }
}
