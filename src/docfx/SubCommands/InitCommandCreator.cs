// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("init", "Generate an initial docfx.json following the instructions")]
    internal sealed class InitCommandCreator : CommandCreator<InitCommandOptions, InitCommand>
    {
        public override InitCommand CreateCommand(InitCommandOptions options, ISubCommandController controller)
        {
            return new InitCommand(options);
        }
    }
}