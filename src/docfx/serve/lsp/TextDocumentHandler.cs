// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Microsoft.Docs.Build
{
    internal class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private readonly ILanguageServerFacade _languageServer;
        private readonly Channel<FileActionEvent> _channel;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.{md,yml,json}",
            });

        public TextDocumentHandler(ILanguageServerFacade languageServer, Channel<FileActionEvent> channel)
        {
            _languageServer = languageServer;
            _channel = channel;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            _channel.Writer.TryWrite(
                new FileActionEvent(FileActionType.Updated, notification.TextDocument.Uri.GetFileSystemPath(), notification.ContentChanges.First().Text));

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
            _channel.Writer.TryWrite(
                new FileActionEvent(FileActionType.Opened, notification.TextDocument.Uri.GetFileSystemPath(), notification.TextDocument.Text));
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
    }
}
