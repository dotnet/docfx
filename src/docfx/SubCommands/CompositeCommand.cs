// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System.Collections.Generic;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json.Linq;

    internal class CompositeCommand : ISubCommand
    {
        public string Name { get; } = nameof(CompositeCommand);

        public bool AllowReplay => true;

        public IList<ISubCommand> Commands { get; } = new List<ISubCommand>();

        public CompositeCommand(string[] args, ISubCommandController controller, CompositeOptions options)
        {
            var result = ToOrderedKeyValuePair(options.ConfigFile);
            foreach (var pair in result)
            {
                ISubCommandCreator command;
                if (!controller.TryGetCommandCreator(pair.Key, out command))
                {
                    Logger.LogWarning($"{pair.Key} is not a recognized command name, ignored.");
                }
                else
                {
                    Commands.Add(command.Create(args, controller, SubCommandParseOption.Loose));
                }
            }
        }

        public void Exec(SubCommandRunningContext context)
        {
            foreach (var command in Commands)
            {
                using (new LoggerPhaseScope(command.Name, LogLevel.Info))
                {
                    command.Exec(context);
                }
            }
        }

        private IEnumerable<KeyValuePair<string, JToken>> ToOrderedKeyValuePair(string path)
        {
            var jObject = CommandUtility.GetConfig<JObject>(path);
            foreach (var item in jObject)
            {
                yield return item;
            }
        }
    }
}
