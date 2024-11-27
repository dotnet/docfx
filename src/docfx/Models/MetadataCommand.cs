// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Docfx.Dotnet;
using Spectre.Console.Cli;

namespace Docfx;

internal class MetadataCommand : Command<MetadataCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] MetadataCommandOptions options)
    {
        return CommandHelper.Run(options, () =>
        {
            var (config, baseDirectory) = Docset.GetConfig(options.Config);
            MergeOptionsToConfig(options, config);
            DotnetApiCatalog.Exec(config.metadata, new(), baseDirectory, options.OutputFolder).GetAwaiter().GetResult();
        });
    }

    private static void MergeOptionsToConfig(MetadataCommandOptions options, DocfxConfig config)
    {
        var msbuildProperties = ResolveMSBuildProperties(options);
        foreach (var item in config.metadata)
        {
            item.ShouldSkipMarkup |= options.ShouldSkipMarkup;
            item.DisableGitFeatures |= options.DisableGitFeatures;
            item.DisableDefaultFilter |= options.DisableDefaultFilter;
            item.NoRestore |= options.NoRestore;
            item.CategoryLayout = options.CategoryLayout ?? item.CategoryLayout;
            item.NamespaceLayout = options.NamespaceLayout ?? item.NamespaceLayout;
            item.MemberLayout = options.MemberLayout ?? item.MemberLayout;
            item.OutputFormat = options.OutputFormat ?? item.OutputFormat;

            if (!string.IsNullOrEmpty(options.FilterConfigFile))
            {
                item.Filter = Path.GetFullPath(options.FilterConfigFile);
            }

            if (!string.IsNullOrEmpty(options.GlobalNamespaceId))
            {
                item.GlobalNamespaceId = options.GlobalNamespaceId;
            }

            if (item.Properties == null)
            {
                item.Properties = msbuildProperties;
            }
            else
            {
                // Command line properties overwrites the one defined in docfx.json
                foreach (var pair in msbuildProperties)
                {
                    item.Properties[pair.Key] = pair.Value;
                }
            }
        }
    }

    private static Dictionary<string, string> ResolveMSBuildProperties(MetadataCommandOptions options)
    {
        var properties = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(options.MSBuildProperties))
        {
            foreach (var pair in options.MSBuildProperties.Split(';'))
            {
                var index = pair.IndexOf('=');
                if (index > -1)
                {
                    // Latter one overwrites former one
                    properties[pair.Substring(0, index)] = pair.Substring(index + 1, pair.Length - index - 1);
                }
            }
        }

        return properties;
    }
}
