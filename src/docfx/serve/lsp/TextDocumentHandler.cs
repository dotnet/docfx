// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Microsoft.Docs.Build
{
    internal class TextDocumentHandler : ITextDocumentSyncHandler
    {
        private readonly ILanguageServerFacade _languageServer;

        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.{md,yml,json}",
            });

        public TextDocumentHandler(ILanguageServerFacade languageServer)
        {
            _languageServer = languageServer;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public Task<Unit> Handle(DidChangeTextDocumentParams notification, CancellationToken token)
        {
            _languageServer.Window.ShowMessage(new ShowMessageParams()
            {
                Type = MessageType.Info,
                Message = $"File change detected on file `{System.IO.Path.GetFileName(notification.TextDocument.Uri.GetFileSystemPath())}` "
                        + $"with latest content '{TakeSubString(notification.ContentChanges.FirstOrDefault().Text, 10)}'",
            });
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
            _languageServer.Window.ShowMessage(new ShowMessageParams()
            {
                Type = MessageType.Info,
                Message = $"File open detected on file `{System.IO.Path.GetFileName(notification.TextDocument.Uri.GetFileSystemPath())}` "
                        + $"with content '{TakeSubString(notification.TextDocument.Text, 10)}'",
            });
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

        private static string TakeSubString(string str, int length)
        {
            if (str.Length > length)
            {
                return $"{str.Substring(0, length)}...";
            }
            else
            {
                return str;
            }
        }
    }
}
