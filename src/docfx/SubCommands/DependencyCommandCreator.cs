// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("dependency", "Export dependency file")]
    internal sealed class DependencyCommandCreator : CommandCreator<DependencyCommandOptions, DependencyCommand>
    {
        public override DependencyCommand CreateCommand(DependencyCommandOptions options, ISubCommandController controller)
        {
            return new DependencyCommand(options);
        }
    }
}
