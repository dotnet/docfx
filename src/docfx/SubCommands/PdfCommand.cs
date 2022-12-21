// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

using Newtonsoft.Json;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands
{
    internal sealed class PdfCommand : ISubCommand
    {
        private readonly PdfJsonConfig _config;

        public string Name { get; } = nameof(PdfCommand);
        public bool AllowReplay => true;

        public PdfCommand(PdfCommandOptions options)
        {
            _config = ParseOptions(options);
        }

        public void Exec(SubCommandRunningContext context)
        {
            Exec(_config);
        }

        public static void Exec(PdfJsonConfig config)
        {
            RunPdf.Exec(config);
        }

        private static PdfJsonConfig ParseOptions(PdfCommandOptions options)
        {
            var configFile = BuildCommand.GetConfigFilePath(options);
            PdfJsonConfig config;
            if (configFile == null)
            {
                if (options.Content == null && options.Resource == null)
                {
                    throw new OptionParserException("Either provide config file or specify content files to start building documentation.");
                }

                config = new PdfJsonConfig
                {
                    BaseDirectory = EnvironmentContext.BaseDirectory
                };
                MergeOptionsToConfig(options, config);
                return config;
            }

            config = CommandUtility.GetConfig<PdfConfig>(configFile).Item;
            if (config == null)
            {
                var message = $"Unable to find pdf subcommand config in file '{configFile}'.";
                Logger.LogError(message, code: ErrorCodes.Config.PdfConfigNotFound);
                throw new DocumentException(message);
            }

            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, config);
            return config;
        }

        private static void MergeOptionsToConfig(PdfCommandOptions options, PdfJsonConfig config)
        {
            BuildCommand.MergeOptionsToConfig(options, config);

            if (options.ExcludedTocs?.Count > 0)
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
}
