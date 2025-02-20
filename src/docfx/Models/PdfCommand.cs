// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Pdf;
using Spectre.Console.Cli;

#nullable enable

namespace Docfx;

internal class PdfCommand : CancellableCommandBase<PdfCommandOptions>
{
    public override int Execute(CommandContext context, PdfCommandOptions options, CancellationToken cancellationToken)
    {
        return CommandHelper.Run(options, () =>
        {
            var (config, configDirectory) = Docset.GetConfig(options.ConfigFile);

            if (config.build is not null)
                PdfBuilder.Run(config.build, configDirectory, options.OutputFolder, cancellationToken).GetAwaiter().GetResult();
        });
    }
}
