// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.HtmlToPdf;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class PdfCommand : ISubCommand
    {
        private readonly BuildCommand _innerBuildCommand;
        private readonly PdfJsonConfig _config;

        public string Name { get; } = nameof(PdfCommand);
        public bool AllowReplay => true;

        public PdfCommand(PdfCommandOptions options)
        {
            ConvertWrapper.PrerequisiteCheck();
            _config = ParseOptions(options);
            if (_config.Serve == true)
            {
                Logger.LogWarning("--serve is not supported in pdf command, ignored");
                _config.Serve = false;
            }

            if (_config.Templates == null || _config.Templates.Count == 0)
            {
                _config.Templates = new ListWithStringFallback(new List<string> { "pdf.default" });
            }

            _innerBuildCommand = new BuildCommand(_config);
        }

        public void Exec(SubCommandRunningContext context)
        {
            EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(_config.BaseDirectory) ? Directory.GetCurrentDirectory() : _config.BaseDirectory));
            // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
            var baseDirectory = EnvironmentContext.BaseDirectory;
            var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(_config.OutputFolder) ? baseDirectory : _config.OutputFolder, _config.Destination ?? string.Empty));
            var rawOutputFolder = string.IsNullOrEmpty(_config.RawOutputFolder) ? Path.Combine(outputFolder, "_raw") : _config.RawOutputFolder;
            var options = new PdfOptions
            {
                BasePath = _config.BasePath,
                CssFilePath = _config.CssFilePath,
                DestDirectory = outputFolder,
                Host = _config.Host,
                Locale = _config.Locale,
                NeedGeneratePdfExternalLink = _config.GeneratesExternalLink,
                GenerateAppendices = _config.GeneratesAppendices,
                PdfConvertParallelism = _config.MaxParallelism == null || _config.MaxParallelism <= 0 ? Environment.ProcessorCount : _config.MaxParallelism.Value,
                PdfDocsetName = _config.Name ?? Path.GetFileName(EnvironmentContext.BaseDirectory),
                SourceDirectory = Path.Combine(rawOutputFolder, _config.Destination ?? string.Empty),
                ExcludeTocs = _config.ExcludedTocs?.ToArray(),
                KeepRawFiles = _config.KeepRawFiles,
                LoadErrorHandling = _config.LoadErrorHandling,
                AdditionalPdfCommandArgs = _config.Wkhtmltopdf?.AdditionalArguments
            };

            // 1. call BuildCommand to generate html files first
            // Output build command exec result to temp folder
            _innerBuildCommand.Config.OutputFolder = rawOutputFolder;

            _innerBuildCommand.Exec(context);

            // 2. call html2pdf converter
            var converter = new ConvertWrapper(options);
            try
            {
                using (new LoggerPhaseScope("PDF", LogLevel.Info))
                {
                    Logger.LogInfo("Start generating PDF files...");
                    converter.Convert();
                }
            }
            catch (IOException ioe)
            {
                throw new DocfxException(ioe.Message, ioe);
            }

            // 3. Should we delete generated files according to manifest
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

                config = new PdfJsonConfig()
                {
                    BaseDirectory = EnvironmentContext.BaseDirectory
                };
                MergeOptionsToConfig(options, config);
                return config;
            }

            config = CommandUtility.GetConfig<PdfConfig>(configFile).Item;
            if (config == null)
            {
                throw new DocumentException($"Unable to find pdf subcommand config in file '{configFile}'.");
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

            if (options.GeneratesExternalLink.HasValue)
            {
                config.GeneratesExternalLink = options.GeneratesExternalLink.Value;
            }
        }

        private sealed class PdfConfig
        {
            [JsonProperty("pdf")]
            public PdfJsonConfig Item { get; set; }
        }
    }
}
