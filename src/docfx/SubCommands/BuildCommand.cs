// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal sealed class BuildCommand : ISubCommand
    {
        internal readonly string BaseDirectory;
        internal readonly string OutputFolder;

        public string Name { get; } = nameof(BuildCommand);

        public BuildJsonConfig Config { get; }

        public bool AllowReplay => true;

        public BuildCommand(BuildCommandOptions options)
        {
            Config = ParseOptions(options, out BaseDirectory, out OutputFolder);
        }

        public void Exec(SubCommandRunningContext context)
        {
            RunBuild.Exec(Config, new(), BaseDirectory, OutputFolder);
        }

        private static BuildJsonConfig ParseOptions(BuildCommandOptions options, out string baseDirectory, out string outputFolder)
        {
            var configFile = GetConfigFilePath(options);

            BuildJsonConfig config;
            if (configFile == null)
            {
                if (options.Content == null && options.Resource == null)
                {
                    throw new OptionParserException("Either provide config file or specify content files to start building documentation.");
                }

                config = new BuildJsonConfig();
                baseDirectory = string.IsNullOrEmpty(configFile) ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(configFile));
                outputFolder = options.OutputFolder;
                MergeOptionsToConfig(options, config, baseDirectory);
                return config;
            }

            config = CommandUtility.GetConfig<BuildConfig>(configFile).Item;
            if (config == null)
            {
                var message = $"Unable to find build subcommand config in file '{configFile}'.";
                Logger.LogError(message, code: ErrorCodes.Config.BuildConfigNotFound);
                throw new DocumentException(message);
            }

            baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configFile));
            outputFolder = options.OutputFolder;
            MergeOptionsToConfig(options, config, baseDirectory);
            return config;
        }

        internal static string GetConfigFilePath(BuildCommandOptions options)
        {
            var configFile = options.ConfigFile;
            if (string.IsNullOrEmpty(configFile))
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    return null;
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, $"Config file {Constants.ConfigFileName} is found.");
                    return Constants.ConfigFileName;
                }
            }

            return configFile;
        }

        internal static void MergeOptionsToConfig(BuildCommandOptions options, BuildJsonConfig config, string configDirectory)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Directory.GetCurrentDirectory();

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

            if (options.XRefMaps != null)
            {
                config.XRefMaps =
                    new ListWithStringFallback(
                        (config.XRefMaps ?? new ListWithStringFallback())
                        .Concat(options.XRefMaps)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());
            }

            if (options.XRefService != null)
            {
                config.XRefServiceUrls =
                    new ListWithStringFallback(
                        (config.XRefServiceUrls ?? new ListWithStringFallback())
                        .Concat(options.XRefService)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());
            }

            //to-do: get changelist from options

            if (options.Serve)
            {
                config.Serve = options.Serve;
            }
            if (options.Host != null)
            {
                config.Host = options.Host;
            }
            if (options.Port.HasValue)
            {
                config.Port = options.Port.Value.ToString();
            }
            config.EnableDebugMode |= options.EnableDebugMode;
            config.ExportRawModel |= options.ExportRawModel;
            config.ExportViewModel |= options.ExportViewModel;

            if (!string.IsNullOrEmpty(options.OutputFolderForDebugFiles))
            {
                config.OutputFolderForDebugFiles = Path.GetFullPath(options.OutputFolderForDebugFiles);
            }
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
            if (options.GlobalMetadataFilePaths != null && options.GlobalMetadataFilePaths.Any())
            {
                config.GlobalMetadataFilePaths.AddRange(options.GlobalMetadataFilePaths);
            }

            config.GlobalMetadataFilePaths =
                new ListWithStringFallback(config.GlobalMetadataFilePaths.Select(
                    path => PathUtility.IsRelativePath(path) ? Path.Combine(configDirectory, path) : path).Reverse());

            if (options.FileMetadataFilePaths != null && options.FileMetadataFilePaths.Any())
            {
                config.FileMetadataFilePaths.AddRange(options.FileMetadataFilePaths);
            }

            config.LruSize = options.LruSize ?? config.LruSize;

            config.KeepFileLink |= options.KeepFileLink;
            config.DisableGitFeatures |= options.DisableGitFeatures;

            config.FileMetadataFilePaths =
                new ListWithStringFallback(config.FileMetadataFilePaths.Select(
                    path => PathUtility.IsRelativePath(path) ? Path.Combine(configDirectory, path) : path).Reverse());

            config.FileMetadata = GetFileMetadataFromOption(config.FileMetadata, options.FileMetadataFilePath, config.FileMetadataFilePaths);
            config.GlobalMetadata = GetGlobalMetadataFromOption(config.GlobalMetadata, options.GlobalMetadataFilePath, config.GlobalMetadataFilePaths, options.GlobalMetadata);
            config.FALName = options.FALName ?? config.FALName;
        }

        internal static Dictionary<string, FileMetadataPairs> GetFileMetadataFromOption(Dictionary<string, FileMetadataPairs> fileMetadataFromConfig, string fileMetadataFilePath, ListWithStringFallback fileMetadataFilePaths)
        {
            var fileMetadata = new Dictionary<string, FileMetadataPairs>();

            if (fileMetadataFilePaths != null)
            {
                foreach (var filePath in fileMetadataFilePaths)
                {
                    fileMetadata = MergeMetadataFromFile("fileMetadata", fileMetadata, filePath, path => JsonUtility.Deserialize<Dictionary<string, FileMetadataPairs>>(path, GetToObjectSerializer()), MergeFileMetadataPairs);
                }
            }

            if (fileMetadataFilePath != null)
            {
                fileMetadata = MergeMetadataFromFile("fileMetadata", fileMetadata, fileMetadataFilePath, path => JsonUtility.Deserialize<BuildJsonConfig>(path, GetToObjectSerializer())?.FileMetadata, MergeFileMetadataPairs);
            }

            return OptionMerger.MergeDictionary(
                new DictionaryMergeContext<FileMetadataPairs>("fileMetadata from docfx config file", fileMetadataFromConfig),
                new DictionaryMergeContext<FileMetadataPairs>("fileMetadata from fileMetadata config file", fileMetadata),
                MergeFileMetadataPairs);
        }

        internal static Dictionary<string, object> GetGlobalMetadataFromOption(Dictionary<string, object> globalMetadataFromConfig, string globalMetadataFilePath, ListWithStringFallback globalMetadataFilePaths, string globalMetadataContent)
        {
            Dictionary<string, object> globalMetadata = null;
            if (globalMetadataContent != null)
            {
                using var sr = new StringReader(globalMetadataContent);
                try
                {
                    globalMetadata = JsonUtility.Deserialize<Dictionary<string, object>>(sr, GetToObjectSerializer());
                }
                catch (JsonException e)
                {
                    Logger.LogWarning($"Metadata from \"--globalMetadata {globalMetadataContent}\" is not a valid JSON format global metadata, ignored: {e.Message}");
                }
            }

            if (globalMetadataFilePaths != null)
            {
                foreach (var filePath in globalMetadataFilePaths)
                {
                    globalMetadata = MergeMetadataFromFile("globalMetadata", globalMetadata, filePath, path => JsonUtility.Deserialize<Dictionary<string, object>>(path, GetToObjectSerializer()), MergeGlobalMetadataItem);
                }
            }

            if (globalMetadataFilePath != null)
            {
                globalMetadata = MergeMetadataFromFile("globalMetadata", globalMetadata, globalMetadataFilePath, path => JsonUtility.Deserialize<BuildJsonConfig>(path)?.GlobalMetadata, MergeGlobalMetadataItem);
            }

            return OptionMerger.MergeDictionary(
                new DictionaryMergeContext<object>("globalMetadata from docfx config file", globalMetadataFromConfig),
                new DictionaryMergeContext<object>("globalMetadata merged with command option and globalMetadata config file", globalMetadata),
                MergeGlobalMetadataItem);
        }

        private static Dictionary<string, T> MergeMetadataFromFile<T>(
            string metadataType,
            Dictionary<string, T> originalMetadata,
            string metadataFilePath,
            Func<string, Dictionary<string, T>> metadataFileLoader,
            OptionMerger.Merger<T> merger)
        {
            Dictionary<string, T> metadata = null;
            try
            {
                if (metadataFilePath != null)
                {
                    metadata = metadataFileLoader(metadataFilePath);
                }

                if (metadata == null)
                {
                    Logger.LogWarning($"File from \"{metadataType} config file {metadataFilePath}\" does not contain \"{metadataType}\" definition.");
                }
            }
            catch (FileNotFoundException)
            {
                Logger.LogWarning($"Invalid option \"{metadataType} config file {metadataFilePath}\": file does not exist, ignored.");
            }
            catch (JsonException e)
            {
                Logger.LogWarning($"File from \"{metadataType} config file {metadataFilePath}\" is not in valid JSON format, ignored: {e.Message}");
            }

            if (metadata != null)
            {
                return OptionMerger.MergeDictionary(
                    new DictionaryMergeContext<T>($"globalMetadata config file {metadataFilePath}", metadata),
                    new DictionaryMergeContext<T>("previous global metadata", originalMetadata),
                    merger);
            }

            return originalMetadata;
        }

        private static object MergeGlobalMetadataItem(string key, MergeContext<object> item, MergeContext<object> overrideItem)
        {
            Logger.LogWarning($"Both {item.Name} and {overrideItem.Name} contain definition for \"{key}\", the one from \"{overrideItem.Name}\" overrides the one from \"{item.Name}\".");
            return overrideItem.Item;
        }

        private static FileMetadataPairs MergeFileMetadataPairs(string key, MergeContext<FileMetadataPairs> pairs, MergeContext<FileMetadataPairs> overridePairs)
        {
            var mergedItems = pairs.Item.Items.Concat(overridePairs.Item.Items).ToList();
            return new FileMetadataPairs(mergedItems);
        }

        private static JsonSerializer GetToObjectSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            jsonSerializer.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializer.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            jsonSerializer.Converters.Add(new JObjectDictionaryToObjectDictionaryConverter());
            return jsonSerializer;
        }

        private sealed class BuildConfig
        {
            [JsonProperty("build")]
            public BuildJsonConfig Item { get; set; }
        }
    }
}
