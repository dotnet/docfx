// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly LanguageServerBuilder _languageServerBuilder;
        private readonly ILanguageServerNotificationListener _notificationListener;
        private readonly LanguageServerPackage _package;

        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.{md,yml,json}",
            });

        public TextDocumentHandler(
            LanguageServerBuilder languageServerBuilder, ILanguageServerNotificationListener notificationListener, LanguageServerPackage package)
        {
            _languageServerBuilder = languageServerBuilder;
            _notificationListener = notificationListener;
            _package = package;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            if (!TryUpdatePackage(notification.TextDocument.Uri, notification.ContentChanges.First().Text))
            {
                _notificationListener.OnNotificationHandled();
                return Unit.Task;
            }

            _languageServerBuilder.QueueBuild();
            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams notification, CancellationToken token)
        {
            if (!TryUpdatePackage(notification.TextDocument.Uri, notification.TextDocument.Text))
            {
                _notificationListener.OnNotificationHandled();
                return Unit.Task;
            }

            _languageServerBuilder.QueueBuild();
            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams notification, CancellationToken token)
        {
            if (!TryRemoveFileFromPackage(notification.TextDocument.Uri))
            {
                _notificationListener.OnNotificationHandled();
                return Unit.Task;
            }
            _languageServerBuilder.QueueBuild();
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams notification, CancellationToken token)
        {
            _notificationListener.OnNotificationHandled();
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

        TextDocumentRegistrationOptions IRegistration<TextDocumentRegistrationOptions>.GetRegistrationOptions()
        {
            return new TextDocumentRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
            };
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

        private bool TryUpdatePackage(DocumentUri file, string? content)
        {
            var filePath = new PathString(file.GetFileSystemPath());
            if (!filePath.StartsWithPath(_package.BasePath, out _))
            {
                return false;
            }

            _package.AddOrUpdate(filePath, content ?? "");
            return true;
        }

        private bool TryRemoveFileFromPackage(DocumentUri file)
        {
            var filePath = new PathString(file.GetFileSystemPath());
            if (!filePath.StartsWithPath(_package.BasePath, out _))
            {
                return false;
            }

            _package.RemoveFile(filePath);
            return true;
        }
    }
}
