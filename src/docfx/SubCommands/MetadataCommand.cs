// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Metadata.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal sealed class MetadataCommand : ISubCommand
    {
        public bool AllowReplay => true;

        public MetadataJsonConfig Config { get; }
        public IEnumerable<ExtractMetadataInputModel> InputModels { get; }

        public MetadataCommand(MetadataCommandOptions options)
        {
            Config = ParseOptions(options);
            InputModels = GetInputModels(Config);
        }

        public void Exec(SubCommandRunningContext context)
        {
            string originalGlobalNamespaceId = VisitorHelper.GlobalNamespaceId;
            EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(Config.BaseDirectory) ? Directory.GetCurrentDirectory() : Config.BaseDirectory));
            foreach (var inputModel in InputModels)
            {
                VisitorHelper.GlobalNamespaceId = inputModel.GlobalNamespaceId;

                // TODO: Use plugin to generate metadata for files with different extension?
                using (var worker = new ExtractMetadataWorker(inputModel, inputModel.ForceRebuild, inputModel.UseCompatibilityFileName))
                {
                    // Use task.run to get rid of current context (causing deadlock in xunit)
                    var task = Task.Run(worker.ExtractMetadataAsync);
                    task.Wait();
                }
            }
            EnvironmentContext.Clean();
            VisitorHelper.GlobalNamespaceId = originalGlobalNamespaceId;
        }

        private MetadataJsonConfig ParseOptions(MetadataCommandOptions options)
        {
            MetadataJsonConfig config;

            string configFile;
            if (TryGetJsonConfig(options.Projects, out configFile))
            {
                config = CommandUtility.GetConfig<MetadataConfig>(configFile).Item;
                if (config == null) throw new DocumentException($"Unable to find metadata subcommand config in file '{configFile}'.");
                config.BaseDirectory = Path.GetDirectoryName(configFile);
            }
            else
            {
                config = new MetadataJsonConfig();
                config.Add(new MetadataJsonItemConfig
                {
                    Destination = options.OutputFolder,
                    Source = new FileMapping(new FileMappingItem(options.Projects.ToArray())) { Expanded = true }
                });
            }

            var msbuildProperties = ResolveMSBuildProperties(options);
            foreach (var item in config)
            {
                item.Force |= options.ForceRebuild;
                item.Raw |= options.PreserveRawInlineComments;
                item.ShouldSkipMarkup |= options.ShouldSkipMarkup;
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

            config.OutputFolder = options.OutputFolder;

            return config;
        }

        private IEnumerable<ExtractMetadataInputModel> GetInputModels(MetadataJsonConfig configs)
        {
            foreach (var config in configs)
            {
                config.Raw |= configs.Raw;
                config.Force |= configs.Force;
                config.ShouldSkipMarkup |= configs.ShouldSkipMarkup;
                yield return ConvertToInputModel(config);
            }
        }

        private ExtractMetadataInputModel ConvertToInputModel(MetadataJsonItemConfig configModel)
        {
            var projects = configModel.Source;
            // If Root Output folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(Config.OutputFolder ?? Config.BaseDirectory ?? string.Empty, configModel.Destination ?? DocAsCode.Constants.DefaultMetadataOutputFolderName);
            var inputModel = new ExtractMetadataInputModel
            {
                PreserveRawInlineComments = configModel?.Raw ?? false,
                ForceRebuild = configModel?.Force ?? false,
                ShouldSkipMarkup = configModel?.ShouldSkipMarkup ?? false,
                ApiFolderName = string.Empty,
                FilterConfigFile = configModel?.FilterConfigFile,
                GlobalNamespaceId = configModel?.GlobalNamespaceId,
                UseCompatibilityFileName = configModel?.UseCompatibilityFileName ?? false,
                MSBuildProperties = configModel?.MSBuildProperties
            };

            var expandedFileMapping = GlobUtility.ExpandFileMapping(Config.BaseDirectory, projects);
            inputModel.Items = new Dictionary<string, List<string>>
            {
                [outputFolder] = expandedFileMapping.Items.SelectMany(s => s.Files).ToList(),
            };

            return inputModel;
        }

        /// <summary>
        /// <n1>=<v1>;<n2>=<v2>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
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
            if (!projects.Any())
            {
                if (!File.Exists(DocAsCode.Constants.ConfigFileName))
                {
                    throw new ArgumentException("Either provide config file or specify project files to generate metadata.");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"Config file {DocAsCode.Constants.ConfigFileName} found, start generating metadata...");
                    jsonConfig = DocAsCode.Constants.ConfigFileName;
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
                    Logger.Log(LogLevel.Warning, $"Multiple {DocAsCode.Constants.ConfigFileName} files are found! The first one \"{jsonConfig}\" is selected, and others \"{string.Join(", ", configFiles.Skip(1))}\" are ignored.");
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
}
