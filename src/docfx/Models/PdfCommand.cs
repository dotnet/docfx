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

            if (config.pdf is not null)
            {
                MergeOptionsToConfig(options, config.pdf, configDirectory);
                RunPdf.Exec(config.pdf, new(), configDirectory, options.OutputFolder);
            }
        });
    }

    private static void MergeOptionsToConfig(PdfCommandOptions options, PdfJsonConfig config, string configDirectory)
    {
        BuildCommand.MergeOptionsToConfig(options, config, configDirectory);

        if (options.ExcludedTocs is not null && options.ExcludedTocs.Any())
        {
            config.ExcludedTocs = new ListWithStringFallback(options.ExcludedTocs);
        }

        if (!string.IsNullOrEmpty(options.CssFilePath))
        {
            config.Css = options.CssFilePath;
        }

        if (!string.IsNullOrEmpty(options.Name))
        {
            config.Name = options.Name;
        }

        if (!string.IsNullOrEmpty(options.Host))
        {
            config.Host = options.Host;
        }

        if (!string.IsNullOrEmpty(options.Locale))
        {
            config.Locale = options.Locale;
        }

        if (!string.IsNullOrEmpty(options.BasePath))
        {
            config.Base = options.BasePath;
        }

        if (!string.IsNullOrEmpty(options.RawOutputFolder))
        {
            config.RawOutputFolder = options.RawOutputFolder;
        }

        if (!string.IsNullOrEmpty(options.LoadErrorHandling))
        {
            config.ErrorHandling = options.LoadErrorHandling;
        }

        if (options.GeneratesAppendices.HasValue)
        {
            config.GeneratesAppendices = options.GeneratesAppendices.Value;
        }

        if (options.KeepRawFiles.HasValue)
        {
            config.KeepRawFiles = options.KeepRawFiles.Value;
        }

        if (options.ExcludeDefaultToc.HasValue)
        {
            config.ExcludeDefaultToc = options.ExcludeDefaultToc.Value;
        }

        if (options.GeneratesExternalLink.HasValue)
        {
            config.GeneratesExternalLink = options.GeneratesExternalLink.Value;
        }

        if (options.NoInputStreamArgs.HasValue)
        {
            config.NoStdin = options.NoInputStreamArgs.Value;
        }

        if (!string.IsNullOrEmpty(options.FilePath))
        {
            config.Wkhtmltopdf.FilePath = Path.Combine(Environment.CurrentDirectory, options.FilePath);
        }
    }
}
