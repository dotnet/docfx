// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Docfx;

internal class MergeCommand : Command<MergeCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] MergeCommandOptions options)
    {
        return CommandHelper.Run(options, () =>
        {
            var config = ParseOptions(options, out var baseDirectory, out var outputFolder);
            RunMerge.Exec(config, baseDirectory);
        });
    }

    private static MergeJsonConfig ParseOptions(MergeCommandOptions options, out string baseDirectory, out string outputFolder)
    {
        (var config, baseDirectory) = Docset.GetConfig(options.Config);

        for (int i = 0; i < config.merge.Count; i++)
        {
            var round = config.merge[i];
            MergeOptionsToConfig(options, ref round);
        }

        outputFolder = options.OutputFolder;
        return config.merge;
    }

    private static void MergeOptionsToConfig(MergeCommandOptions options, ref MergeJsonItemConfig config)
    {
        // base directory for content from command line is current directory
        // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
        // for `--content "*.cs*`, base directory should be `C:\folder1`
        // hence GetFullPath used
        if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
    }
}
