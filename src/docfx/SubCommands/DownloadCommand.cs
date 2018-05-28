// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class DownloadCommand : ISubCommand
    {
        private readonly DownloadCommandOptions _options;

        public string Name { get; } = nameof(DownloadCommand);

        public bool AllowReplay => true;

        public DownloadCommand(DownloadCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            if (string.IsNullOrWhiteSpace(_options.ArchiveFile))
            {
                Logger.LogError("Please provide output file.");
                return;
            }
            var builder = new XRefArchiveBuilder();
            if (Uri.TryCreate(_options.Uri, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                builder.DownloadAsync(uri, _options.ArchiveFile).Wait();
            }
            else
            {
                Logger.LogError($"Invalid uri: {_options.Uri}");
            }
        }
    }
}
