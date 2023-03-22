// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("metadata", "Generate YAML files from source code")]
internal sealed class MetadataCommandCreator : CommandCreator<MetadataCommandOptions, MetadataCommand>
{
    public override MetadataCommand CreateCommand(MetadataCommandOptions options, ISubCommandController controller)
    {
        return new MetadataCommand(options);
    }
}
