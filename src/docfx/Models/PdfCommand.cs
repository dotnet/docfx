// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace Docfx;

internal class PdfCommand : Command<PdfCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] PdfCommandOptions options)
    {
        return CommandHelper.Run(options, () =>
        {
            var (config, baseDirectory) = CommandHelper.GetConfig<PdfConfig>(options.ConfigFile);
            MergeOptionsToConfig(options, config.Item, baseDirectory);
            RunPdf.Exec(config.Item, new(), baseDirectory, options.OutputFolder);
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

    private sealed class PdfConfig
    {
        [JsonProperty("pdf")]
        [JsonPropertyName("pdf")]
        public PdfJsonConfig Item { get; set; }
    }
}
