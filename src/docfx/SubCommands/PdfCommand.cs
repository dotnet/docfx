// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

internal static class PdfCommand
{
    public static void Exec(PdfCommandOptions options)
    {
        var Config = ParseOptions(options, out var BaseDirectory, out var OutputFolder);
        RunPdf.Exec(Config, new(), BaseDirectory, OutputFolder);
    }

    private static PdfJsonConfig ParseOptions(PdfCommandOptions options, out string baseDirectory, out string outputFolder)
    {
        var configFile = BuildCommand.GetConfigFilePath(options);

        PdfJsonConfig config;
        if (configFile == null)
        {
            if (options.Content == null && options.Resource == null)
            {
                throw new OptionParserException("Either provide config file or specify content files to start building documentation.");
            }

            config = new PdfJsonConfig();
            baseDirectory = string.IsNullOrEmpty(configFile) ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(Path.GetFullPath(configFile));
            outputFolder = options.OutputFolder;
            MergeOptionsToConfig(options, config, baseDirectory);
            return config;
        }

        config = CommandUtility.GetConfig<PdfConfig>(configFile).Item;
        if (config == null)
        {
            var message = $"Unable to find pdf subcommand config in file '{configFile}'.";
            Logger.LogError(message, code: ErrorCodes.Config.PdfConfigNotFound);
            throw new DocumentException(message);
        }

        baseDirectory = Path.GetDirectoryName(Path.GetFullPath(configFile));
        outputFolder = options.OutputFolder;
        MergeOptionsToConfig(options, config, baseDirectory);
        return config;
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
