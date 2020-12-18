// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly ILanguageServerNotificationListener _notificationListener;

        public DiagnosticPublisher(ILanguageServerFacade languageServer, ILanguageServerNotificationListener notificationListener)
        {
            _languageServer = languageServer;
            _notificationListener = notificationListener;
        }

        public void PublishDiagnostic(PathString file, List<Diagnostic> diagnostics)
        {
            _languageServer.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
            {
                Uri = DocumentUri.File(file),
                Diagnostics = new Container<Diagnostic>(diagnostics),
            });

            _notificationListener.OnNotificationSent();
        }
    }
}
