// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.ComponentModel;
    using System.Linq;
    using System.Composition.Hosting;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;

    internal class CommandController : ISubCommandController
    {
        [ImportMany]
        private IList<ExportFactory<ISubCommandCreator, BaseCommandOption>> _allCommands { get; set; }

        private Dictionary<string, ExportFactory<ISubCommandCreator, BaseCommandOption>> _commandMapping { get; }

        public CommandController(string pluginFolder)
        {
            GetContainer(pluginFolder).SatisfyImports(this);

            var commands = _allCommands.GroupBy(s => s.Metadata.Name).ToList();

            var duplicateCommands = commands.Where(s => s.Count() > 1);
            if (duplicateCommands.Any())
            {
                Logger.LogWarning($"More than one subcommands with the following names are found: \"{string.Join(",", duplicateCommands.Select(s => s.Key))}\", a random one will be picked up. Note that subcommands names are case-sensitive");
            }

            _commandMapping = commands.ToDictionary(s => s.Key, s => s.First());
        }

        public bool TryGetCommandCreator(string name, out ISubCommandCreator creator)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            ExportFactory<ISubCommandCreator, BaseCommandOption> commandFactory;
            if (_commandMapping.TryGetValue(name, out commandFactory))
            {
                creator = commandFactory.CreateExport().Value;
                return true;
            }

            creator = null;
            return false;
        }

        public ISubCommand Create(string[] args, ISubCommandController controller, SubCommandParseOption option)
        {
            if (args.Length > 0)
            {
                // Drawback: do not support such case: docfx --force <subcommands>
                var subCommandName = args[0];
                ISubCommandCreator command;
                if (TryGetCommandCreator(subCommandName, out command))
                {
                    var subArgs = args.Skip(1).ToArray();
                    return command.Create(subArgs, controller, SubCommandParseOption.Strict);
                }
            }
            var options = new CompositeOptions();
            bool parsed = ArgsParser.LooseParser.ParseArguments(args, options);
            if (options.IsHelp) return new HelpCommand(controller.GetHelpText());
            return new CompositeSubCommand(args, controller, options);
        }

        public string GetHelpText()
        {
            // "command name": "command help text"
            return string.Join(Environment.NewLine, new string[] { HelpTextGenerator.GetHeader() }.Concat(_commandMapping.Select(s => $"{s.Key}: {s.Value.Metadata.HelpText};")));
        }

        private CompositionHost GetContainer(string pluginFolder)
        {
            var configuration = new ContainerConfiguration();
            configuration.WithAssembly(typeof(CommandFactory).Assembly);
            var pluginDir = Path.Combine(Path.GetDirectoryName(typeof(CommandFactory).Assembly.Location), pluginFolder);
            if (Directory.Exists(pluginDir))
            {
                foreach (var file in Directory.EnumerateFiles(pluginDir, "*.dll"))
                {
                    configuration.WithAssembly(Assembly.LoadFile(file));
                }
            }
            return configuration.CreateContainer();
        }

        private class BaseCommandOption
        {
            [DefaultValue("")]
            public string Name { get; set; }
            [DefaultValue("")]
            public string HelpText { get; set; }
        }
    }
}
