// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.Docs.Build
{
    internal class DiagnosticPublisher
    {
        private readonly ILanguageServerFacade _languageServer;
        private readonly ConcurrentDictionary<DocumentUri, DateTime> _fileDiagnosticLastUpdateTime = new ConcurrentDictionary<DocumentUri, DateTime>();

        public DiagnosticPublisher(ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
        }

        public void PublishDiagnostic(PathString file, List<Diagnostic> diagnostics, DateTime? timeStamp = null)
            => PublishDiagnostic(DocumentUri.File(file), diagnostics, timeStamp);

        public void PublishDiagnostic(DocumentUri file, List<Diagnostic> diagnostics, DateTime? timeStamp = null)
        {
            timeStamp ??= DateTime.UtcNow;

            var lastUpdateTime = _fileDiagnosticLastUpdateTime.GetOrAdd(file, (_) => (DateTime)timeStamp);
            if (timeStamp >= lastUpdateTime)
            {
                _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
                {
                    Uri = file,
                    Diagnostics = new Container<Diagnostic>(diagnostics),
                });
            }
        }
    }
}
