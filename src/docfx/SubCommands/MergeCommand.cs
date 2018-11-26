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
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

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

        public string Name { get; } = nameof(MergeCommand);
        public MergeJsonConfig Config { get; }
        public bool AllowReplay => true;

        public MergeCommand(MergeCommandOptions options)
        {
            Config = ParseOptions(options);
        }

        public void Exec(SubCommandRunningContext context)
        {
            var config = Config;
            foreach (var round in config)
            {
                var baseDirectory = round.BaseDirectory ?? Directory.GetCurrentDirectory();
                var intermediateOutputFolder = round.Destination ?? Path.Combine(baseDirectory, "obj");
                EnvironmentContext.SetBaseDirectory(baseDirectory);
                EnvironmentContext.SetOutputDirectory(intermediateOutputFolder);
                MergeDocument(baseDirectory, intermediateOutputFolder);
                EnvironmentContext.Clean();
            }
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
                        var item = new MergeJsonItemConfig();
                        MergeOptionsToConfig(options, ref item);
                        config.Add(item);
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
            for (int i = 0; i < config.Count; i++)
            {
                var round = config[i];
                round.BaseDirectory = Path.GetDirectoryName(configFile);

                MergeOptionsToConfig(options, ref round);
            }

            return config;
        }

        private static void MergeOptionsToConfig(MergeCommandOptions options, ref MergeJsonItemConfig config)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Directory.GetCurrentDirectory();

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
            config.FileMetadata = BuildCommand.GetFileMetadataFromOption(config.FileMetadata, options.FileMetadataFilePath, null);
            config.GlobalMetadata = BuildCommand.GetGlobalMetadataFromOption(config.GlobalMetadata, options.GlobalMetadataFilePath, null, options.GlobalMetadata);
            if (options.TocMetadata != null)
            {
                config.TocMetadata = new ListWithStringFallback(options.TocMetadata);
            }
        }

        private sealed class MergeConfig
        {
            [JsonProperty("merge")]
            public MergeJsonConfig Item { get; set; }
        }

        #endregion

        private void MergeDocument(string baseDirectory, string outputDirectory)
        {
            foreach (var round in Config)
            {
                var parameters = ConfigToParameter(round, baseDirectory, outputDirectory);
                if (parameters.Files.Count == 0)
                {
                    Logger.LogWarning("No files found, nothing is to be generated");
                    continue;
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
        }

        private static MetadataMergeParameters ConfigToParameter(MergeJsonItemConfig config, string baseDirectory, string outputDirectory) =>
            new MetadataMergeParameters
            {
                OutputBaseDir = outputDirectory,
                Metadata = config.GlobalMetadata?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty,
                FileMetadata = ConvertToFileMetadataItem(baseDirectory, config.FileMetadata),
                TocMetadata = config.TocMetadata?.ToImmutableList() ?? ImmutableList<string>.Empty,
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
                   select Path.Combine(file.SourceFolder ?? Directory.GetCurrentDirectory(), item);
        }

        private static FileCollection GetFileCollectionFromFileMapping(string baseDirectory, DocumentType type, FileMapping files)
        {
            var result = new FileCollection(baseDirectory);
            foreach (var mapping in files.Items)
            {
                result.Add(type, mapping.Files, mapping.SourceFolder, mapping.DestinationFolder);
            }
            return result;
        }
    }
}
