// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("merge", "Merge .net base API in YAML files and toc files.")]
    internal sealed class MergeCommandCreator : CommandCreator<MergeCommandOptions, MergeCommand>
    {
        public override MergeCommand CreateCommand(MergeCommandOptions options, ISubCommandController controller)
        {
            return new MergeCommand(options);
        }
    }
}
