// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Spectre.Console.Cli;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DocAsCode;

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
            config.CssFilePath = options.CssFilePath;
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
            config.BasePath = options.BasePath;
        }

        if (!string.IsNullOrEmpty(options.RawOutputFolder))
        {
            config.RawOutputFolder = options.RawOutputFolder;
        }

        if (!string.IsNullOrEmpty(options.LoadErrorHandling))
        {
            config.LoadErrorHandling = options.LoadErrorHandling;
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
            config.NoInputStreamArgs = options.NoInputStreamArgs.Value;
        }

        if (!string.IsNullOrEmpty(options.FilePath))
        {
            config.Wkhtmltopdf.FilePath = Path.Combine(Environment.CurrentDirectory, options.FilePath);
        }
    }

    private sealed class PdfConfig
    {
        [JsonProperty("pdf")]
        public PdfJsonConfig Item { get; set; }
    }
}
