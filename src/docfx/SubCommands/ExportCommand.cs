// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;

    /// <summary>
    /// TODO: NOT SURE IF IT IS WORKING NOW, simply migrate from old sub command and have not done any E2E test
    /// </summary>
    internal class ExportCommand : ISubCommand
    {
        private readonly ExportCommandOptions _options;
        private readonly MetadataCommand _metadataCommand;
        public bool AllowReplay => true;

        public ExportCommand(ExportCommandOptions options)
        {
            _options = options;
            _metadataCommand = new MetadataCommand(options);
        }

        public void Exec(SubCommandRunningContext context)
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
                AddProjects(package, inputModels.SelectMany(s => s.Items).Select(s => s.Key).ToList());
            }
        }

        public void AddProjects(ExternalReferencePackageWriter writer, IReadOnlyList<string> projectPaths)
        {
            if (projectPaths == null)
            {
                throw new ArgumentNullException(nameof(projectPaths));
            }
            if (projectPaths.Count == 0)
            {
                throw new ArgumentException("Empty collection is not allowed.", nameof(projectPaths));
            }
            for (int i = 0; i < projectPaths.Count; i++)
            {
                var name = Path.GetFileName(projectPaths[i]);
                ExternalReferencePackageHelper.AddFiles(
                    writer,
                    new Uri(_options.BaseUrl),
                    _options.UrlPattern,
                    string.Empty,
                    Directory.GetFiles(Path.Combine(projectPaths[i], "api"), "*.yml", SearchOption.TopDirectoryOnly));
            }
        }
    }
}
