// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using Newtonsoft.Json.Linq;

    internal class CompositeSubCommand : ISubCommand
    {
        public IList<ISubCommand> Commands { get; } = new List<ISubCommand>();

        public CompositeSubCommand(string[] args, ISubCommandController controller, CompositeOptions options)
        {
            if (string.IsNullOrEmpty(options.ConfigFile))
            {
                options.ConfigFile = DocAsCode.Constants.ConfigFileName;
            }
            if (!File.Exists(options.ConfigFile)) throw new FileNotFoundException($"Config file {options.ConfigFile} does not exist!");
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
                command.Exec(context);
            }
        }

        private IEnumerable<KeyValuePair<string, JToken>> ToOrderedKeyValuePair(string path)
        {
            var jObject = JsonUtility.Deserialize<JObject>(path);
            foreach (var item in jObject)
            {
                yield return item;
            }
        }
    }
}
