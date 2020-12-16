// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.Docs.Build
{
    internal class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private static readonly string[] s_configFileNames =
            {
                "docfx.json",
                "docfx.yml",
                ".openpublishing.publish.config.json",
                "docsets.json",
                "docsets.yml",
                "redirections.yml",
                "redirections.json",
                ".openpublishing.redirection.json",
            };

        private readonly LanguageServerBuilder _languageServerBuilder;
        private readonly LanguageServerPackage _package;

        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.{md,yml,json}",
            });

        public TextDocumentHandler(LanguageServerBuilder languageServerBuilder, LanguageServerPackage package)
        {
            _languageServerBuilder = languageServerBuilder;
            _package = package;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            if (TryUpdatePackage(notification.TextDocument.Uri, notification.ContentChanges.First().Text))
            {
                _languageServerBuilder.QueueBuild();
            }

            return Unit.Task;
        }

        TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentChangeRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                SyncKind = Change,
            };
        }

        public void SetCapability(SynchronizationCapability capability)
        {
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
        {
            if (TryUpdatePackage(notification.TextDocument.Uri, notification.TextDocument.Text))
            {
                _languageServerBuilder.QueueBuild();
            }
            return Unit.Task;
        }

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
            };
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
        {
            return Unit.Task;
        }

        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentSaveRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                IncludeText = true,
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new TextDocumentAttributes(uri, "docfx");
        }

        private bool TryUpdatePackage(DocumentUri file, string content)
        {
            var filePath = new PathString(file.GetFileSystemPath());
            if (!filePath.StartsWithPath(_package.BasePath, out _))
            {
                return false;
            }

            var fileName = Path.GetFileName(filePath);
            if (s_configFileNames.Contains(fileName, PathUtility.PathComparer))
            {
                return false;
            }

            _package.AddOrUpdate(filePath, content);
            return true;
        }
    }
}
