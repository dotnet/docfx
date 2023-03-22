// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("build", "Generate client-only website combining API in YAML files and conceptual files")]
internal sealed class BuildCommandCreator : CommandCreator<BuildCommandOptions, BuildCommand>
{
    public override BuildCommand CreateCommand(BuildCommandOptions options, ISubCommandController controller)
    {
        return new BuildCommand(options);
    }
}
