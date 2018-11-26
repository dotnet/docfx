// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.ComponentModel;
    using System.Composition.Hosting;
    using System.Linq;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal class CommandController : ISubCommandController
    {
        [ImportMany]
        private IList<ExportFactory<ISubCommandCreator, BaseCommandOption>> _allCommands { get; set; }

        private readonly Dictionary<string, ExportFactory<ISubCommandCreator, BaseCommandOption>> _commandMapping;

        private readonly string[] _args;

        public CommandController(string[] args)
        {
            GetContainer().SatisfyImports(this);

            var commands = _allCommands.GroupBy(s => s.Metadata.Name).ToList();

            var duplicateCommands = commands.Where(s => s.Count() > 1);
            if (duplicateCommands.Any())
            {
                Logger.LogWarning($"More than one subcommands with the following names are found: \"{string.Join(",", duplicateCommands.Select(s => s.Key))}\", a random one will be picked up. Note that subcommands names are case-sensitive");
            }

            _commandMapping = commands.ToDictionary(s => s.Key, s => s.First());
            _args = args;
        }

        public bool TryGetCommandCreator(string name, out ISubCommandCreator creator)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (_commandMapping.TryGetValue(name, out var commandFactory))
            {
                creator = commandFactory.CreateExport().Value;
                return true;
            }

            creator = null;
            return false;
        }

        public ISubCommand Create()
        {
            var args = _args;
            if (args.Length > 0)
            {
                // Drawback: do not support such case: docfx --force <subcommands>
                var subCommandName = args[0];
                if (TryGetCommandCreator(subCommandName, out ISubCommandCreator command))
                {
                    var subArgs = args.Skip(1).ToArray();
                    return command.Create(subArgs, this, SubCommandParseOption.Strict);
                }
            }

            // TODO: comment: also handle log and loglevel like in build command
            // WONT FIX: log and loglevel will be handled in each sub-command as passed in by options
            var options = new CompositeOptions();
            var parser = CommandUtility.GetParser(SubCommandParseOption.Loose);
            bool parsed = parser.ParseArguments(args, options);
            if (options.ShouldShowVersion)
            {
                return new HelpCommand(GetVersionText());
            }
            if (options.IsHelp)
            {
                return new HelpCommand(GetHelpText());
            }
            return new CompositeCommand(args, this, options);
        }

        public string GetVersionText()
        {
            return HelpTextGenerator.GetVersion();
        }

        public string GetHelpText()
        {
            // "command name": "command help text"
            return string.Join(
                Environment.NewLine,
                new string[] { HelpTextGenerator.GetHeader() }.Concat(GetSubCommandHelpTextLines(_commandMapping.Select(s => s.Value.Metadata))));
        }

        private IEnumerable<string> GetSubCommandHelpTextLines(IEnumerable<BaseCommandOption> options)
        {
            int maxLength = options.Max(s => s.Name.Length);
            int leftWidth = maxLength + 6;
            int paddingLeft = 4;
            return options.OrderBy(s => s.Name).Select(s => GetSubCommandHelpText(s, paddingLeft, leftWidth));
        }

        private string GetSubCommandHelpText(BaseCommandOption option, int paddingLeft, int leftWidth)
        {
            return option.Name.PadRight(leftWidth).PadLeft(leftWidth + paddingLeft) + ": " + option.HelpText;
        }

        private CompositionHost GetContainer()
        {
            var configuration = new ContainerConfiguration();
            configuration.WithAssembly(typeof(CommandController).Assembly);
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
