// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Spectre.Console.Cli;

#nullable enable

namespace Docfx;

internal class CleanCommand : Command<CleanCommandOptions>
{
    public override int Execute(CommandContext context, CleanCommandOptions settings, CancellationToken cancellationToken)
    {
        return CommandHelper.Run(settings, () =>
        {
            // Gets docfx config path.
            var configPath = string.IsNullOrEmpty(settings.ConfigFile)
                               ? DataContracts.Common.Constants.ConfigFileName
                               : settings.ConfigFile!;
            configPath = Path.GetFullPath(configPath);

            // Load configs
            var (config, baseDirectory) = Docset.GetConfig(configPath);

            // Gets output directories
            var buildOutputDirectory = GetBuildOutputDirectory(config, baseDirectory);
            var metadataOutputDirectories = GetMetadataOutputDirectories(config, baseDirectory);

            RunClean.Exec(new RunCleanContext
            {
                ConfigDirectory = Path.GetDirectoryName(configPath)!,
                BuildOutputDirectory = buildOutputDirectory,
                MetadataOutputDirectories = metadataOutputDirectories,
                DryRun = settings.DryRun,
            });
        });
    }

    /// <summary>
    /// Gets output directory of `docfx build` command.
    /// </summary>
    internal static string GetBuildOutputDirectory(DocfxConfig config, string baseDirectory)
    {
        var buildConfig = config.build;
        if (buildConfig == null)
            return "";

        // Combine path
        var outputDirectory = Path.Combine(baseDirectory, buildConfig.Output ?? buildConfig.Dest ?? "");

        // Normalize to full path 
        return Path.GetFullPath(outputDirectory);
    }

    /// <summary>
    /// Gets output directories of `docfx metadata` command.
    /// </summary>
    internal static string[] GetMetadataOutputDirectories(DocfxConfig config, string baseDirectory)
    {
        var metadataConfig = config.metadata;
        if (metadataConfig == null)
            return [];

        return metadataConfig.Select(x =>
        {
            var outputDirectory = Path.Combine(baseDirectory, x.Output ?? x.Dest ?? "");
            return Path.GetFullPath(outputDirectory);
        }).ToArray();
    }
}
