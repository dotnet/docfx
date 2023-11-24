// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Docfx.Build.Engine;
using Docfx.Common;
using Spectre.Console.Cli;

namespace Docfx;

internal class DownloadCommand : Command<DownloadCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] DownloadCommandOptions options)
    {
        return CommandHelper.Run(() =>
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
        });
    }
}
