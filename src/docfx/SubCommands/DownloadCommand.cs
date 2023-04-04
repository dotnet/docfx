// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.SubCommands;

internal static class DownloadCommand
{
    public static void Exec(DownloadCommandOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ArchiveFile))
        {
            Logger.LogError("Please provide output file.");
            return;
        }
        var builder = new XRefArchiveBuilder();
        if (Uri.TryCreate(options.Uri, UriKind.RelativeOrAbsolute, out Uri uri))
        {
            builder.DownloadAsync(uri, options.ArchiveFile).Wait();
        }
        else
        {
            Logger.LogError($"Invalid uri: {options.Uri}");
        }
    }
}
