// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    // TODO: currently disabled
    // [CommandOption("pack", "Pack exsiting YAML files to external reference")]
    internal sealed class PackCommandCreator : CommandCreator<PackCommandOptions, PackCommand>
    {
        public override PackCommand CreateCommand(ExportCommandOptions options, ISubCommandController controller)
        {
            return new PackCommand(options);
        }
    }
}
