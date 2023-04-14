// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode.SubCommands;

internal class DownloadCommand : Command<DownloadCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] DownloadCommandOptions options)
    {
        var builder = new XRefArchiveBuilder();
        if (Uri.TryCreate(options.Uri, UriKind.RelativeOrAbsolute, out Uri uri))
        {
            builder.DownloadAsync(uri, options.ArchiveFile).Wait();
        }
        else
        {
            Logger.LogError($"Invalid uri: {options.Uri}");
        }
        return 0;
    }
}
