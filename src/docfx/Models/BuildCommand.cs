// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Docfx;

internal class BuildCommand : Command<BuildCommandOptions>
{
    public override int Execute(CommandContext context, BuildCommandOptions settings)
    {
        return CommandHelper.Run(settings, () =>
        {
            if (settings.Serve && CommandHelper.IsTcpPortAlreadyUsed(settings.Host, settings.Port))
            {
                Logger.LogError($"Serve option specified. But TCP port {settings.Port ?? 8080} is already being in use.");
                return;
            }

            var (config, baseDirectory) = Docset.GetConfig(settings.ConfigFile);
            MergeOptionsToConfig(settings, config.build, baseDirectory);
            var serveDirectory = RunBuild.Exec(config.build, new(), baseDirectory, settings.OutputFolder);

            if (settings.Serve)
                RunServe.Exec(serveDirectory, settings.Host, settings.Port, settings.OpenBrowser, settings.OpenFile);
        });
    }

    internal static void MergeOptionsToConfig(BuildCommandOptions options, BuildJsonConfig config, string configDirectory)
    {
        // base directory for content from command line is current directory
        // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
        // for `--content "*.cs*`, base directory should be `C:\folder1`
        // hence GetFullPath used below

        // Override config file with options from command line
        if (options.Templates != null && options.Templates.Any())
        {
            config.Template = new ListWithStringFallback(options.Templates);
        }

        if (options.PostProcessors != null && options.PostProcessors.Any())
        {
            config.PostProcessors = new ListWithStringFallback(options.PostProcessors);
        }

        if (options.Themes != null && options.Themes.Any())
        {
            config.Theme = new ListWithStringFallback(options.Themes);
        }

        if (options.XRefMaps != null)
        {
            config.Xref =
                new ListWithStringFallback(
                    (config.Xref ?? [])
                    .Concat(options.XRefMaps)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct());
        }

        config.Debug |= options.EnableDebugMode;
        config.ExportRawModel |= options.ExportRawModel;
        config.ExportViewModel |= options.ExportViewModel;

        if (!string.IsNullOrEmpty(options.OutputFolderForDebugFiles))
        {
            config.DebugOutput = Path.GetFullPath(options.OutputFolderForDebugFiles);
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
            config.MarkdownEngineProperties = JsonConvert.DeserializeObject<MarkdownServiceProperties>(options.MarkdownEngineProperties);
        }

        config.GlobalMetadataFiles =
            new ListWithStringFallback(config.GlobalMetadataFiles.Select(
                path => PathUtility.IsRelativePath(path) ? Path.Combine(configDirectory, path) : path).Reverse());

        SetGlobalMetadataFromCommandLineArgs();

        config.DisableGitFeatures |= options.DisableGitFeatures;

        void SetGlobalMetadataFromCommandLineArgs()
        {
            if (options.Metadata != null)
            {
                config.GlobalMetadata ??= [];
                foreach (var metadata in options.Metadata)
                {
                    var (key, value) = ParseMetadata(metadata);
                    config.GlobalMetadata[key] = value;
                }
            }

            static (string key, object value) ParseMetadata(string metadata)
            {
                if (metadata.IndexOf('=') is int i && i < 0)
                    return (metadata, true);

                var key = metadata.Substring(0, i);
                var value = metadata.Substring(i + 1);

                if (bool.TryParse(value, out var boolean))
                    return (key, boolean);

                return (key, value);
            }
        }
    }
}
