// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Exceptions;

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

        public void Exec(RunningContext context)
        {
            if (_helpMessage != null)
            {
                Console.WriteLine(_helpMessage);
            }
            else
            {
                InternalExec(context);
            }
        }

        private void InternalExec(RunningContext context)
        {
            _metadataCommand.Exec(context);

            // 2. convert.
            var inputModels = _metadataCommand.InputModels;
            var outputFile = Path.Combine(_options.OutputFolder ?? Environment.CurrentDirectory, _options.Name ?? "externalreference.rpk");
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new InvalidOptionException("BaseUrl cannot be empty.", "BaseUrl");
            }

            var baseUri = new Uri(_options.BaseUrl);
            if (!baseUri.IsAbsoluteUri)
            {
                throw new InvalidOptionException("BaseUrl should be absolute url.", "BaseUrl");
            }
            using (var package = _options.AppendMode ? ExternalReferencePackageWriter.Append(outputFile, baseUri) : ExternalReferencePackageWriter.Create(outputFile, baseUri))
            {
                package.AddProjects(inputModels.SelectMany(s => s.Items).Select(s => s.Key).ToList());
            }
        }
    }
}
