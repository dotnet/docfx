// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// TODO: NOT SURE IF IT IS WORKING NOW, simply migrate from old sub command and have not done any E2E test
    /// </summary>
    internal class ExportCommand : ICommand
    {
        private string _helpMessage = null;
        private MetadataCommand _metadataCommand;
        public ExportCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public ExportCommand(Options options, CommandContext context)
        {
            _options = options.ExportCommand;
            if (_options.IsHelp)
            {
                _helpMessage = HelpTextGenerator.GetHelpMessage(options, "export");
            }
            else
            {
                options.MetadataCommand = _options;
                _metadataCommand = new MetadataCommand(options, context);
            }
        }

        public ParseResult Exec(RunningContext context)
        {
            if (_helpMessage != null)
            {
                Console.WriteLine(_helpMessage);
                return ParseResult.SuccessResult;
            }

            var extractMetadataResult = _metadataCommand.Exec(context);

            Logger.Log(extractMetadataResult);
            if (extractMetadataResult.ResultLevel == ResultLevel.Error)
            {
                return extractMetadataResult;
            }

            // 2. convert.
            var inputModels = _metadataCommand.InputModels;
            var outputFile = Path.Combine(_options.OutputFolder ?? Environment.CurrentDirectory, _options.Name ?? "externalreference.rpk");
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                return new ParseResult(ResultLevel.Error, "BaseUrl cannot be empty.");
            }
            try
            {
                var baseUri = new Uri(_options.BaseUrl);
                if (!baseUri.IsAbsoluteUri)
                {
                    return new ParseResult(ResultLevel.Error, "BaseUrl should be absolute url.");
                }
                using (var package = _options.AppendMode ? ExternalReferencePackageWriter.Append(outputFile, baseUri) : ExternalReferencePackageWriter.Create(outputFile, baseUri))
                {
                    package.AddProjects(inputModels.SelectMany(s => s.Items).Select(s => s.Key).ToList());
                }

                return ParseResult.SuccessResult;
            }
            catch (Exception ex)
            {
                return new ParseResult(ResultLevel.Error, ex.ToString());
            }
        }
    }
}
