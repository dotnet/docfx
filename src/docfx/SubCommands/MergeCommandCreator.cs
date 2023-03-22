// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("merge", "Merge .net base API in YAML files and toc files.")]
internal sealed class MergeCommandCreator : CommandCreator<MergeCommandOptions, MergeCommand>
{
    public override MergeCommand CreateCommand(MergeCommandOptions options, ISubCommandController controller)
    {
        return new MergeCommand(options);
    }
}
