// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.MetadataMergers;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;
    internal sealed class MergeCommand : ISubCommand
    {
        private static JsonSerializer GetSerializer() =>
            new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                Converters =
                {
                    new JObjectDictionaryToObjectDictionaryConverter(),
                }
            };

        public MergeJsonConfig Config { get; }

        public MergeCommand(MergeCommandOptions options)
        {
            Config = ParseOptions(options);
        }

        public void Exec(SubCommandRunningContext context)
        {
            var config = Config;
            var baseDirectory = config.BaseDirectory ?? Environment.CurrentDirectory;
            var intermediateOutputFolder = Path.Combine(baseDirectory, "obj");

            MergeDocument(baseDirectory, intermediateOutputFolder);
        }

        #region MergeCommand ctor related

        private static MergeJsonConfig ParseOptions(MergeCommandOptions options)
        {
            var configFile = options.ConfigFile;
            MergeJsonConfig config;
            if (string.IsNullOrEmpty(configFile))
            {
                if (!File.Exists(DocAsCode.Constants.ConfigFileName))
                {
                    if (options.Content == null)
                    {
                        throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                    }
                    else
                    {
                        config = new MergeJsonConfig();
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

            config = CommandUtility.GetConfig<MergeConfig>(configFile).Item;
            if (config == null) throw new DocumentException($"Unable to find build subcommand config in file '{configFile}'.");
            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, ref config);

            return config;
        }

        private static void MergeOptionsToConfig(MergeCommandOptions options, ref MergeJsonConfig config)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Environment.CurrentDirectory;

            config.OutputFolder = options.OutputFolder;

            if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
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
                        SourceFolder = optionsBaseDirectory
                    });
            }
            var fileMetadata = BuildCommand.GetFileMetadataFromOption(options.FileMetadataFilePath);
            if (fileMetadata != null)
            {
                config.FileMetadata = fileMetadata;
            }
            var globalMetadata = BuildCommand.GetGlobalMetadataFromOption(options.GlobalMetadata, options.GlobalMetadataFilePath);
            if (globalMetadata != null)
            {
                config.GlobalMetadata = globalMetadata;
            }
        }

        private static Dictionary<string, object> GetGlobalMetadataFromOption(MergeCommandOptions options)
        {
            Dictionary<string, object> globalMetadata = null;
            if (options.GlobalMetadata != null)
            {
                using (var sr = new StringReader(options.GlobalMetadata))
                {
                    try
                    {
                        globalMetadata = JsonUtility.Deserialize<Dictionary<string, object>>(sr, GetSerializer());
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
                    var globalMetadataFromFile = JsonUtility.Deserialize<MergeJsonConfig>(options.GlobalMetadataFilePath).GlobalMetadata;
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

        private sealed class MergeConfig
        {
            [JsonProperty("merge")]
            public MergeJsonConfig Item { get; set; }
        }

        #endregion

        private void MergeDocument(string baseDirectory, string outputDirectory)
        {
            var parameters = ConfigToParameter(Config, baseDirectory, outputDirectory);
            if (parameters.Files.Count == 0)
            {
                Logger.LogWarning("No files found, nothing is to be generated");
                return;
            }
            try
            {
                new MetadataMerger().Merge(parameters);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
        }

        private static MetadataMergeParameters ConfigToParameter(MergeJsonConfig config, string baseDirectory, string outputDirectory) =>
            new MetadataMergeParameters
            {
                OutputBaseDir = outputDirectory,
                Metadata = config.GlobalMetadata?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty,
                FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata),
                Files = GetFileCollectionFromFileMapping(
                    baseDirectory,
                    DocumentType.Article,
                    GlobUtility.ExpandFileMapping(baseDirectory, config.Content)),
            };

        private static FileMetadata ConvertToFileMetadataItem(string baseDirectory, Dictionary<string, FileMetadataPairs> fileMetadata)
        {
            if (fileMetadata == null)
            {
                return null;
            }
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
            if (mapping == null)
            {
                return Enumerable.Empty<string>();
            }
            return from file in mapping.Items
                   from item in file.Files
                   select Path.Combine(file.SourceFolder ?? Environment.CurrentDirectory, item);
        }

        private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, DocumentType type, FileMapping files)
        {
            var result = new FileCollection(baseDirectory);
            foreach (var mapping in files.Items)
            {
                result.Add(type, mapping.Files, s => ConvertToDestinationPath(Path.Combine(baseDirectory, s), mapping.SourceFolder, mapping.DestinationFolder));
            }
            return result;
        }

        private static string ConvertToDestinationPath(string path, string src, string dest)
        {
            var relativePath = PathUtility.MakeRelativePath(src, path);
            return Path.Combine(dest ?? string.Empty, relativePath);
        }
    }
}
