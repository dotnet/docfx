// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Docfx.Pdf;
using Spectre.Console.Cli;

namespace Docfx;

internal class PdfCommand : Command<PdfCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] PdfCommandOptions options)
    {
        return CommandHelper.Run(options, () =>
        {
            var (config, configDirectory) = Docset.GetConfig(options.ConfigFile);

            if (config.build is not null)
                PdfBuilder.Run(config.build, configDirectory, options.OutputFolder).GetAwaiter().GetResult();
        });
    }
}
