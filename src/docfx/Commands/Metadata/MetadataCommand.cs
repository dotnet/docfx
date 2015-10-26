// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class MetadataCommand : ICommand
    {
        private CommandContext _context;
        private string _helpMessage = null;
        public MetadataJsonConfig Config { get; }
        public MetadataCommand(CommandContext context) : this(new MetadataJsonConfig(), context)
        {
        }

        public MetadataCommand(JToken value, CommandContext context) : this(CommandFactory.ConvertJTokenTo<MetadataJsonConfig>(value), context)
        {
        }

        public MetadataCommand(MetadataJsonConfig config, CommandContext context)
        {
            _context = context;
            Config = config;
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
                Config = GetConfigFromOptions(metadataCommandOptions);
                _context = context;
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
            Config.BaseDirectory = _context.BaseDirectory;
            return InternalExec(Config, context);
        }

        private ParseResult InternalExec(MetadataJsonConfig config, RunningContext context)
        {
            // TODO: can we do it parallelly?
            return CompositeCommand.AggregateParseResult(YieldExec(config, context));
        }

        /// <summary>
        /// TODO: catch exception 
        /// </summary>
        /// <param name="configs"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<ParseResult> YieldExec(MetadataJsonConfig configs, RunningContext context)
        {
            foreach (var config in configs)
            {
                var forceRebuild = config.Force;

                var inputModel = ConvertToInputModel(config, configs.BaseDirectory);

                // TODO: Use plugin to generate metadata for files with different extension?
                var worker = new ExtractMetadataWorker(inputModel, forceRebuild);

                yield return worker.ExtractMetadataAsync().Result;

            }
        }

        private ExtractMetadataInputModel ConvertToInputModel(MetadataJsonItemConfig configModel, string baseDirectory)
        {
            var projects = configModel.Source;
            // If Root Output folder is specified from command line, use it instead of the base directory
            var outputFolder = Path.Combine(_context?.RootOutputFolder ?? baseDirectory ?? string.Empty, configModel.Destination ?? Constants.DefaultRootOutputFolderPath);
            var inputModel = new ExtractMetadataInputModel
            {
                PreserveRawInlineComments = configModel.Raw,
            };

            var expandedFileMapping = GlobUtility.ExpandFileMapping(baseDirectory, projects);
            inputModel.Items = new Dictionary<string, List<string>>
            {
                [outputFolder]= expandedFileMapping.Items.SelectMany(s=>s.Files).ToList(),
            };

            return inputModel;
        }

        private static MetadataJsonConfig GetConfigFromOptions(MetadataCommandOptions options)
        {
            string jsonConfig;
            if (CommandFactory.TryGetJsonConfig(options.Projects, out jsonConfig))
            {
                var command = (MetadataCommand)CommandFactory.ReadConfig(jsonConfig, null).Commands.FirstOrDefault(s => s is MetadataCommand);
                if (command == null) throw new ApplicationException($"Unable to find {SubCommandType.Build} subcommand config in file '{Constants.ConfigFileName}'.");
                return command.Config;
            }

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
}
