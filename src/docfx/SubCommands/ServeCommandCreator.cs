// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("serve", "Host a local static website")]
    internal sealed class ServeCommandCreator : CommandCreator<ServeCommandOptions, ServeCommand>
    {
        public override ServeCommand CreateCommand(ServeCommandOptions options, ISubCommandController controller)
        {
            return new ServeCommand(options);
        }
    }
}