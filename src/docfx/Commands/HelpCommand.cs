// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    internal class HelpCommand : ICommand
    {
        public HelpCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public HelpCommand(Options options, CommandContext context)
        {
            _options = options.HelpCommand;
            _rootOptions = options;
        }

        public void Exec(RunningContext context)
        {
            string text = HelpTextGenerator.GetHelpMessage(_rootOptions, _options.Command);
            _rootOptions.CurrentSubCommand = CommandType.Help;
            Console.WriteLine(text);
        }
    }
}
