// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class DownloadCommand : ISubCommand
    {
        private readonly DownloadCommandOptions _options;

        public bool AllowReplay => true;

        public DownloadCommand(DownloadCommandOptions options)
        {
            _options = options;
        }

        public void Exec(SubCommandRunningContext context)
        {
            var builder = new XRefArchiveBuilder();
            Uri uri;
            if (Uri.TryCreate(_options.Uri, UriKind.RelativeOrAbsolute, out uri))
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
