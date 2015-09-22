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
        public MetadataJsonConfig Config { get; }
        public MetadataCommand(): this(new MetadataJsonConfig())
        {
        }

        public MetadataCommand(JToken value): this(CommandFactory.ConvertJTokenTo<MetadataJsonConfig>(value))
        {
        }

        public MetadataCommand(MetadataJsonConfig config)
        {
            Config = config;
        }

        public MetadataCommand(Options options) : this(options.MetadataCommand) { }

        public MetadataCommand(MetadataCommandOptions options) : this(GetConfigFromOptions(options)) { }

        public ParseResult Exec(RunningContext context)
        {
            return InternalExec(Config, context);
        }

        private ParseResult InternalExec(MetadataJsonConfig options, RunningContext context)
        {
            // TODO: can we do it parallelly?
            return CompositeCommand.AggregateParseResult(YieldExec(options, context));
        }

        /// <summary>
        /// TODO: catch exception 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private IEnumerable<ParseResult> YieldExec(MetadataJsonConfig options, RunningContext context)
        {
            foreach (var config in options)
            {
                var forceRebuild = config.Force;

                var inputModel = ConvertToInputModel(config, options.BaseDirectory);

                // TODO: Use plugin to generate metadata for files with different extension?
                var worker = new ExtractMetadataWorker(inputModel, forceRebuild);

                yield return worker.ExtractMetadataAsync().Result;

            }
        }

        private static ExtractMetadataInputModel ConvertToInputModel(MetadataJsonItemConfig configModel, string baseDirectory)
        {
            var projects = configModel.Source;
            var outputFolder = configModel.Destination == null ? Constants.DefaultRootOutputFolderPath : configModel.Destination.ToNormalizedFullPath();
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
                var command = (MetadataCommand)CommandFactory.ReadConfig(jsonConfig).Commands.FirstOrDefault(s => s is MetadataCommand);
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
