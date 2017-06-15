// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;

    using Microsoft.DocAsCode.Plugins;

    internal class HelpCommand : ISubCommand
    {
        private string _message;
        public bool AllowReplay => false;
        public string Name { get; } = nameof(HelpCommand);

        public HelpCommand(string message)
        {
            _message = message;
        }

        public void Exec(SubCommandRunningContext context)
        {
            Console.WriteLine(_message);
        }
    }
}
