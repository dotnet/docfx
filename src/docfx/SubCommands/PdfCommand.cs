// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.HtmlToPdf;
    using Microsoft.DocAsCode.Plugins;
    using Newtonsoft.Json;

    internal sealed class PdfCommand : ISubCommand
    {
        private readonly BuildCommand _innerBuildCommand;
        private readonly PdfJsonConfig _config;
        public bool AllowReplay => true;

        public PdfCommand(PdfCommandOptions options)
        {
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
            var intermediateOutputFolder = Path.Combine(baseDirectory, "obj");
            var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(_config.OutputFolder) ? baseDirectory : _config.OutputFolder, _config.Destination ?? string.Empty));

            var options = new PdfOptions
            {
                BasePath = _config.BasePath,
                CssFilePath = _config.CssFilePath,
                DestDirectory = outputFolder,
                Host = _config.Host,
                Locale = _config.Locale,
                NeedGeneratePdfExternalLink = _config.GeneratePdfExternalLink,
                GenerateAppendices = _config.GenerateAppendices,
                PdfConvertParallelism = _config.MaxParallelism == null || _config.MaxParallelism <= 0 ? Environment.ProcessorCount : _config.MaxParallelism.Value,
                PdfDocsetName = _config.Name ?? Path.GetFileName(EnvironmentContext.BaseDirectory),
                SourceDirectory = outputFolder,
                ExcludeTocs = _config.ExcludedTocs?.ToArray(),
            };

            // 1. call BuildCommand to generate html files first
            _innerBuildCommand.Exec(context);
            // 2. call html2pdf converter
           
            var converter = new ConvertWrapper(options);
            try
            {
                using (new LoggerPhaseScope("Generating PDF", LogLevel.Info))
                {
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
                config = new PdfJsonConfig();
                config.BaseDirectory = EnvironmentContext.BaseDirectory;
                MergeOptionsToConfig(options, config);
                return config;
            }

            config = CommandUtility.GetConfig<PdfConfig>(configFile).Item;
            if (config == null) throw new DocumentException($"Unable to find pdf subcommand config in file '{configFile}'.");
            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, config);
            return config;
        }

        private static void MergeOptionsToConfig(PdfCommandOptions options, PdfJsonConfig config)
        {
            BuildCommand.MergeOptionsToConfig(options, config);

            if (options.ExcludedTocs != null && options.ExcludedTocs.Count > 0)
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

            if (!string.IsNullOrEmpty(options.BasePath))
            {
                config.BasePath = options.BasePath;
            }
        }

        private sealed class PdfConfig
        {
            [JsonProperty("pdf")]
            public PdfJsonConfig Item { get; set; }
        }
    }
}
