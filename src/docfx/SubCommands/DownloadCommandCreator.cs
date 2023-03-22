// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

[CommandOption("download", "Download remote xref map file and create an xref archive in local.")]
internal sealed class DownloadCommandCreator : CommandCreator<DownloadCommandOptions, DownloadCommand>
{
    public override DownloadCommand CreateCommand(DownloadCommandOptions options, ISubCommandController controller)
    {
        return new DownloadCommand(options);
    }
}
