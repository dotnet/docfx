// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    // TODO: currently disabled
    // [CommandOption("export", "Export YAML files to external reference package")]
    internal sealed class ExportCommandCreator : CommandCreator<ExportCommandOptions, ExportCommand>
    {
        public override ExportCommand CreateCommand(ExportCommandOptions options, ISubCommandController controller)
        {
            return new ExportCommand(options);
        }
    }
}
