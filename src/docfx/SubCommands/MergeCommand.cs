// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.SubCommands;

internal static class MergeCommand
{
    public static void Exec(MergeCommandOptions options)
    {
        var config = ParseOptions(options, out var baseDirectory, out var outputFolder);
        RunMerge.Exec(config, baseDirectory);
    }

    private static MergeJsonConfig ParseOptions(MergeCommandOptions options, out string baseDirectory, out string outputFolder)
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

                    baseDirectory = string.IsNullOrEmpty(configFile) ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(configFile));
                    outputFolder = options.OutputFolder;
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
        if (config == null)
        {
            var message = $"Unable to find build subcommand config in file '{configFile}'.";
            Logger.LogError(message, code: ErrorCodes.Config.BuildConfigNotFound);
            throw new DocumentException(message);
        }

        for (int i = 0; i < config.Count; i++)
        {
            var round = config[i];
            MergeOptionsToConfig(options, ref round);
        }

        baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configFile));
        outputFolder = options.OutputFolder;
        return config;
    }

    private static void MergeOptionsToConfig(MergeCommandOptions options, ref MergeJsonItemConfig config)
    {
        // base directory for content from command line is current directory
        // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
        // for `--content "*.cs*`, base directory should be `C:\folder1`
        string optionsBaseDirectory = Directory.GetCurrentDirectory();

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
}
