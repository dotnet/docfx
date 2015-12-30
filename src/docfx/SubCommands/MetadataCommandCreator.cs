// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("metadata", "Generate YAML files from source code")]
    internal sealed class MetadataCommandCreator : CommandCreator<MetadataCommandOptions, MetadataCommand>
    {
        public override MetadataCommand CreateCommand(MetadataCommandOptions options, ISubCommandController controller)
        {
            return new MetadataCommand(options);
        }
    }
}
