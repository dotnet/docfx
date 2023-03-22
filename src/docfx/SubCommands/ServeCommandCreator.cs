// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("serve", "Host a local static website")]
internal sealed class ServeCommandCreator : CommandCreator<ServeCommandOptions, ServeCommand>
{
    public override ServeCommand CreateCommand(ServeCommandOptions options, ISubCommandController controller)
    {
        return new ServeCommand(options);
    }
}