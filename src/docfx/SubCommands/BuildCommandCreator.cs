// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("build", "Generate client-only website combining API in YAML files and conceptual files")]
    internal sealed class BuildCommandCreator : CommandCreator<BuildCommandOptions, BuildCommand>
    {
        public override BuildCommand CreateCommand(BuildCommandOptions options, ISubCommandController controller)
        {
            return new BuildCommand(options);
        }
    }
}
