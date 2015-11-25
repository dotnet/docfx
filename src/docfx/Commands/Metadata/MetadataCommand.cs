// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Plugins;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MetadataCommand : ICommand
    {
        private string _helpMessage = null;
        public MetadataJsonConfig Config { get; }
        public IEnumerable<ExtractMetadataInputModel> InputModels { get; }

        private MetadataJsonConfig MergeConfig(MetadataJsonConfig config, CommandContext context)
        {
            config.BaseDirectory = context?.BaseDirectory ?? config.BaseDirectory;
            config.OutputFolder = context?.RootOutputFolder ?? config.OutputFolder;
            return config;
        }

        public MetadataCommand(CommandContext context) : this(new MetadataJsonConfig(), context)
        {
        }

        public MetadataCommand(JToken value, CommandContext context) : this(CommandFactory.ConvertJTokenTo<MetadataJsonConfig>(value), context)
        {
        }

        public MetadataCommand(MetadataJsonConfig config, CommandContext context)
        {
            Config = MergeConfig(config, context);
            InputModels = GetInputModels(Config, context);
        }

        public MetadataCommand(Options options, CommandContext context)
        {
            var metadataCommandOptions = options.MetadataCommand;
            if (metadataCommandOptions.IsHelp)
            {
                _helpMessage = HelpTextGenerator.GetHelpMessage(options, "metadata");
            }
            else
            {
                Config = MergeConfig(GetConfigFromOptions(metadataCommandOptions), context);
                InputModels = GetInputModels(Config, context);
            }

            if (!string.IsNullOrWhiteSpace(metadataCommandOptions.Log)) Logger.RegisterListener(new ReportLogListener(metadataCommandOptions.Log));

            if (metadataCommandOptions.LogLevel.HasValue) Logger.LogLevelThreshold = metadataCommandOptions.LogLevel.Value;
        }

        public ParseResult Exec(RunningContext context)
        {
            if (_helpMessage != null)
            {
                Console.WriteLine(_helpMessage);
                return ParseResult.SuccessResult;
            }

            return InternalExec(context);
        }

        private ParseResult InternalExec(RunningContext context)
        {
            // TODO: can we do it parallelly?
            return CompositeCommand.AggregateParseResult(YieldExec(context));
        }

        /// <summary>
        /// TODO: catch exception 
        /// </summary>
        /// <param name="configs"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<ParseResult> YieldExec(RunningContext context)
        {
            foreach (var inputModel in InputModels)
            {
                // TODO: Use plugin to generate metadata for files with different extension?
                var worker = new ExtractMetadataWorker(inputModel, inputModel.ForceRebuild);

                yield return worker.ExtractMetadataAsync().Result;
            }
        }

        private IEnumerable<ExtractMetadataInputModel> GetInputModels(MetadataJsonConfig configs, CommandContext context)
        {
            foreach (var config in configs)
            {
                yield return ConvertToInputModel(config);
            }
        }

        private ExtractMetadataInputModel ConvertToInputModel(MetadataJsonItemConfig configModel)
        {
            var projects = configModel.Source;
            // If Root Output folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(Config.OutputFolder ?? Config.BaseDirectory ?? string.Empty, configModel.Destination ?? Constants.DefaultRootOutputFolderPath);
            var inputModel = new ExtractMetadataInputModel
            {
                PreserveRawInlineComments = configModel.Raw,
                ForceRebuild = configModel.Force,
                ApiFolderName = string.Empty,
            };

            var expandedFileMapping = GlobUtility.ExpandFileMapping(Config.BaseDirectory, projects);
            inputModel.Items = new Dictionary<string, List<string>>
            {
                [outputFolder]= expandedFileMapping.Items.SelectMany(s=>s.Files).ToList(),
            };

            return inputModel;
        }

        private static MetadataJsonConfig GetConfigFromOptions(MetadataCommandOptions options)
        {
            string jsonConfig;
            if (TryGetJsonConfig(options.Projects, out jsonConfig))
            {
                var command = (MetadataCommand)CommandFactory.ReadConfig(jsonConfig, null).Commands.FirstOrDefault(s => s is MetadataCommand);
                if (command == null) throw new DocumentException($"Unable to find {CommandType.Build} subcommand config in file '{Constants.ConfigFileName}'.");
                var config = command.Config;
                config.BaseDirectory = Path.GetDirectoryName(jsonConfig);
                config.OutputFolder = options.OutputFolder;
                foreach (var item in config)
                {
                    item.Raw |= options.PreserveRawInlineComments;
                    item.Force |= options.ForceRebuild;
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
                    Source = new FileMapping(new FileMappingItem() { Files = new FileItems(options.Projects) })
                });
                return config;
            }
        }

        private static bool TryGetJsonConfig(List<string> inputGlobPattern, out string jsonConfig)
        {
            var validProjects = FileGlob.GetFiles(string.Empty, inputGlobPattern, null).ToList();

            if (!validProjects.Any())
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    throw new ArgumentException("Either provide config file or specify project files to generate metadata.");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"Config file {Constants.ConfigFileName} found, start generating metadata...");
                    jsonConfig = Constants.ConfigFileName;
                    return true;
                }
            }

            // Get the first docfx.json config file
            var configFiles = validProjects.FindAll(s => Path.GetFileName(s).Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase));
            var otherFiles = validProjects.Except(configFiles).ToList();

            // Load and ONLY load docfx.json when it exists
            if (configFiles.Count > 0)
            {
                jsonConfig = configFiles[0];
                var baseDirectory = Path.GetDirectoryName(jsonConfig);
                if (configFiles.Count > 1)
                {
                    Logger.Log(LogLevel.Warning, $"Multiple {Constants.ConfigFileName} files are found! The first one in {configFiles[0]} is selected, and others are ignored.");
                }
                else
                {
                    if (otherFiles.Count > 0)
                    {
                        Logger.Log(LogLevel.Warning, $"Config file {Constants.ConfigFileName} is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters will be ignored.");
                    }
                    else Logger.Log(LogLevel.Verbose, $"Config file is found in {jsonConfig}.");
                }
                
                return true;
            }
            else
            {
                jsonConfig = null;
                return false;
            }
        }
    }
}
