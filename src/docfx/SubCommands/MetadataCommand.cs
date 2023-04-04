// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Dotnet;
using Microsoft.DocAsCode.Plugins;

using Newtonsoft.Json;

namespace Microsoft.DocAsCode.SubCommands;

internal sealed class MetadataCommand
{
    public static void Exec(MetadataCommandOptions options)
    {
        var config = ParseOptions(options, out var baseDirectory, out var outputFolder);
        DotnetApiCatalog.Exec(config, new(), baseDirectory, outputFolder).GetAwaiter().GetResult();
    }

    private static MetadataJsonConfig ParseOptions(MetadataCommandOptions options, out string baseDirectory, out string outputFolder)
    {
        MetadataJsonConfig config;
        baseDirectory = null;
        if (TryGetJsonConfig(options.Projects.ToList(), out string configFile))
        {
            config = CommandUtility.GetConfig<MetadataConfig>(configFile).Item;
            if (config == null)
            {
                var message = $"Unable to find metadata subcommand config in file '{configFile}'.";
                Logger.LogError(message, code: ErrorCodes.Config.MetadataConfigNotFound);
                throw new DocumentException(message);
            }

            baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configFile));
        }
        else
        {
            config = new MetadataJsonConfig
            {
                new MetadataJsonItemConfig
                {
                    Destination = options.OutputFolder,
                    Source = new FileMapping(new FileMappingItem(options.Projects.ToArray())) { Expanded = true }
                }
            };
        }

        var msbuildProperties = ResolveMSBuildProperties(options);
        foreach (var item in config)
        {
            item.ShouldSkipMarkup |= options.ShouldSkipMarkup;
            item.DisableGitFeatures |= options.DisableGitFeatures;
            item.DisableDefaultFilter |= options.DisableDefaultFilter;
            item.NamespaceLayout = options.NamespaceLayout ?? item.NamespaceLayout;
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

        return config;
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

    private static bool TryGetJsonConfig(List<string> projects, out string jsonConfig)
    {
        if (projects.Count == 0)
        {
            if (!File.Exists(Constants.ConfigFileName))
            {
                throw new OptionParserException("Either provide config file or specify project files to generate metadata.");
            }
            else
            {
                Logger.Log(LogLevel.Info, $"Config file {Constants.ConfigFileName} found, start generating metadata...");
                jsonConfig = Constants.ConfigFileName;
                return true;
            }
        }

        // Get the first docfx.json config file
        var configFiles = projects.FindAll(s => Path.GetExtension(s).Equals(Constants.ConfigFileExtension, StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(s).Equals(Constants.SupportedProjectName));
        var otherFiles = projects.Except(configFiles).ToList();

        // Load and ONLY load docfx.json when it exists
        if (configFiles.Count > 0)
        {
            jsonConfig = configFiles[0];
            if (configFiles.Count > 1)
            {
                Logger.Log(LogLevel.Warning, $"Multiple {Constants.ConfigFileName} files are found! The first one \"{jsonConfig}\" is selected, and others \"{string.Join(", ", configFiles.Skip(1))}\" are ignored.");
            }
            else
            {
                if (otherFiles.Count > 0)
                {
                    Logger.Log(LogLevel.Warning, $"Config file \"{jsonConfig}\" is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters \"{string.Join(", ", otherFiles)}\" will be ignored.");
                }
                else Logger.Log(LogLevel.Verbose, $"Config file \"{jsonConfig}\" is used.");
            }

            return true;
        }
        else
        {
            jsonConfig = null;
            return false;
        }
    }

    private sealed class MetadataConfig
    {
        [JsonProperty("metadata")]
        public MetadataJsonConfig Item { get; set; }
    }
}
