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
            foreach (var inputModel in InputModels)
            {
                // TODO: Use plugin to generate metadata for files with different extension?
                using (var worker = new ExtractMetadataWorker(inputModel, inputModel.ForceRebuild, inputModel.UseCompatibilityFileName))
                {
                    // Use task.run to get rid of current context (causing deadlock in xunit)
                    var task = Task.Run(worker.ExtractMetadataAsync);
                    task.Wait();
                }
            }
        }

        private MetadataJsonConfig ParseOptions(MetadataCommandOptions options)
        {
            string configFile;
            if (TryGetJsonConfig(options.Projects, out configFile))
            {
                var config = CommandUtility.GetConfig<MetadataConfig>(configFile).Item;
                if (config == null) throw new DocumentException($"Unable to find metadata subcommand config in file '{configFile}'.");
                config.BaseDirectory = Path.GetDirectoryName(configFile);
                config.OutputFolder = options.OutputFolder;
                foreach (var item in config)
                {
                    item.Raw |= options.PreserveRawInlineComments;
                    item.Force |= options.ForceRebuild;
                    item.FilterConfigFile = options.FilterConfigFile ?? item.FilterConfigFile;
                }
                return config;
            }
            else
            {
                var config = new MetadataJsonConfig();
                config.Add(new MetadataJsonItemConfig
                {
                    Force = options.ForceRebuild,
                    Destination = options.OutputFolder,
                    Raw = options.PreserveRawInlineComments,
                    Source = new FileMapping(new FileMappingItem(options.Projects.ToArray())) { Expanded = true },
                    FilterConfigFile = options.FilterConfigFile
                });
                return config;
            }
        }

        private IEnumerable<ExtractMetadataInputModel> GetInputModels(MetadataJsonConfig configs)
        {
            foreach (var config in configs)
            {
                config.Raw |= configs.Raw;
                config.Force |= configs.Force;
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
                ApiFolderName = string.Empty,
                FilterConfigFile = configModel?.FilterConfigFile,
                UseCompatibilityFileName = configModel?.UseCompatibilityFileName ?? false,
            };

            var expandedFileMapping = GlobUtility.ExpandFileMapping(Config.BaseDirectory, projects);
            inputModel.Items = new Dictionary<string, List<string>>
            {
                [outputFolder] = expandedFileMapping.Items.SelectMany(s => s.Files).ToList(),
            };

            return inputModel;
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
            var configFiles = projects.FindAll(s => Path.GetFileName(s).Equals(DocAsCode.Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase));
            var otherFiles = projects.Except(configFiles).ToList();

            // Load and ONLY load docfx.json when it exists
            if (configFiles.Count > 0)
            {
                jsonConfig = configFiles[0];
                if (configFiles.Count > 1)
                {
                    Logger.Log(LogLevel.Warning, $"Multiple {DocAsCode.Constants.ConfigFileName} files are found! The first one \"{jsonConfig}\" is selected, and others are ignored.");
                }
                else
                {
                    if (otherFiles.Count > 0)
                    {
                        Logger.Log(LogLevel.Warning, $"Config file \"{jsonConfig}\" is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters will be ignored.");
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
