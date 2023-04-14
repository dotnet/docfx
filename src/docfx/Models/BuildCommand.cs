// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

internal class BuildCommand : Command<BuildCommandOptions>
{
    public override int Execute(CommandContext context, BuildCommandOptions settings)
    {
        return CommandHelper.Run(settings, () =>
        {
            var config = ParseOptions(settings, out var baseDirectory, out var outputFolder);
            RunBuild.Exec(config, new(), baseDirectory, outputFolder);
        });
    }

    private static BuildJsonConfig ParseOptions(BuildCommandOptions options, out string baseDirectory, out string outputFolder)
    {
        (var config, baseDirectory) = CommandHelper.GetConfig<BuildConfig>(options.ConfigFile);
        outputFolder = options.OutputFolder;
        MergeOptionsToConfig(options, config.Item, baseDirectory);
        return config.Item;
    }

    internal static void MergeOptionsToConfig(BuildCommandOptions options, BuildJsonConfig config, string configDirectory)
    {
        // base directory for content from command line is current directory
        // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
        // for `--content "*.cs*`, base directory should be `C:\folder1`
        string optionsBaseDirectory = Directory.GetCurrentDirectory();

        // Override config file with options from command line
        if (options.Templates != null && options.Templates.Any())
        {
            config.Templates = new ListWithStringFallback(options.Templates);
        }

        if (options.PostProcessors != null && options.PostProcessors.Any())
        {
            config.PostProcessors = new ListWithStringFallback(options.PostProcessors);
        }

        if (options.Themes != null && options.Themes.Any())
        {
            config.Themes = new ListWithStringFallback(options.Themes);
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

        config.GlobalMetadataFilePaths =
            new ListWithStringFallback(config.GlobalMetadataFilePaths.Select(
                path => PathUtility.IsRelativePath(path) ? Path.Combine(configDirectory, path) : path).Reverse());

        config.LruSize = options.LruSize ?? config.LruSize;

        config.KeepFileLink |= options.KeepFileLink;
        config.DisableGitFeatures |= options.DisableGitFeatures;
    }

    private sealed class BuildConfig
    {
        [JsonProperty("build")]
        public BuildJsonConfig Item { get; set; }
    }
}
