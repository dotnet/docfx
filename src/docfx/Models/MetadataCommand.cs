// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Docfx.Dotnet;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Docfx;

internal class MetadataCommand : Command<MetadataCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] MetadataCommandOptions options)
    {
        return CommandHelper.Run(options, () =>
        {
            var config = ParseOptions(options, out var baseDirectory, out var outputFolder);
            DotnetApiCatalog.Exec(config, new(), baseDirectory, outputFolder).GetAwaiter().GetResult();
        });
    }

    private static MetadataJsonConfig ParseOptions(MetadataCommandOptions options, out string baseDirectory, out string outputFolder)
    {
        MetadataConfig config;

        if (options.Config != null && !string.Equals(Path.GetFileName(options.Config), DataContracts.Common.Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
        {
            config = new()
            {
                Item = new()
                {
                    new()
                    {
                        Destination = options.OutputFolder,
                        Source = new FileMapping(new FileMappingItem(new[]{ options.Config })) { Expanded = true },
                    }
                }
            };
            baseDirectory = Directory.GetCurrentDirectory();
        }
        else
        {
            (config, baseDirectory) = CommandHelper.GetConfig<MetadataConfig>(options.Config);
        }

        var msbuildProperties = ResolveMSBuildProperties(options);
        foreach (var item in config.Item)
        {
            item.ShouldSkipMarkup |= options.ShouldSkipMarkup;
            item.DisableGitFeatures |= options.DisableGitFeatures;
            item.DisableDefaultFilter |= options.DisableDefaultFilter;
            item.NamespaceLayout = options.NamespaceLayout ?? item.NamespaceLayout;
            item.OutputFormat = options.OutputFormat ?? item.OutputFormat;

            if (!string.IsNullOrEmpty(options.FilterConfigFile))
            {
                item.FilterConfigFile = Path.GetFullPath(options.FilterConfigFile);
            }

            if (!string.IsNullOrEmpty(options.GlobalNamespaceId))
            {
                item.GlobalNamespaceId = options.GlobalNamespaceId;
            }

            if (item.MSBuildProperties == null)
            {
                item.MSBuildProperties = msbuildProperties;
            }
            else
            {
                // Command line properties overwrites the one defined in docfx.json
                foreach (var pair in msbuildProperties)
                {
                    item.MSBuildProperties[pair.Key] = pair.Value;
                }
            }
        }

        outputFolder = options.OutputFolder;

        return config.Item;
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

    private sealed class MetadataConfig
    {
        [JsonProperty("metadata")]
        public MetadataJsonConfig Item { get; set; }
    }
}
