// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

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
