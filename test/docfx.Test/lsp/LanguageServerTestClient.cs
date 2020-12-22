// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using Xunit;
using Yunit;

namespace Microsoft.Docs.Build
{
    internal class LanguageServerTestClient : ILanguageServerNotificationListener, IAsyncDisposable
    {
        private static readonly JsonDiff s_languageServerJsonDiff = CreateLanguageServerJsonDiff();

        private readonly string _workingDirectory;
        private readonly Lazy<Task<ILanguageClient>> _client;

        private readonly ConcurrentDictionary<string, JToken> _diagnostics = new();

        private int _serverNotificationSent;
        private int _serverNotificationHandled;
        private int _clientNotificationSent;
        private int _clientNotificationReceived;
        private int _clientNotificationReceivedBeforeSync; // For expectNoNotification
        private TaskCompletionSource _notificationSync = new TaskCompletionSource();

        public LanguageServerTestClient(string workingDirectory, Package package)
        {
            _workingDirectory = workingDirectory;
            _client = new(() => InitializeClient(workingDirectory, package));
        }

        public async Task ProcessCommand(LanguageServerTestCommand command)
        {
            var client = await _client.Value;

            if (command.OpenFiles != null)
            {
                foreach (var (file, text) in command.OpenFiles)
                {
                    BeforeSendNotification();

                    client.DidOpenTextDocument(new() { TextDocument = new() { Uri = ToUri(file), Text = text } });
                }
            }
            else if (command.EditFiles != null)
            {
                foreach (var (file, text) in command.EditFiles)
                {
                    BeforeSendNotification();

                    client.DidChangeTextDocument(new()
                    {
                        TextDocument = new() { Uri = ToUri(file) },
                        ContentChanges = new(new TextDocumentContentChangeEvent() { Text = text }),
                    });
                }
            }
            else if (command.CloseFiles != null)
            {
                foreach (var file in command.CloseFiles)
                {
                    BeforeSendNotification();

                    client.DidCloseTextDocument(new()
                    {
                        TextDocument = new(ToUri(file)),
                    });
                }
            }
            else if (command.ExpectDiagnostics != null)
            {
                await SynchronizeNotifications();

                s_languageServerJsonDiff.Verify(command.ExpectDiagnostics, _diagnostics);
            }
            else if (command.ExpectNoNotification)
            {
                await SynchronizeNotifications();
                Assert.Equal(_clientNotificationReceivedBeforeSync, _clientNotificationReceived);
            }
            else
            {
                throw new NotSupportedException("Invalid language server test command");
            }
        }

        public async ValueTask DisposeAsync()
        {
            var client = await _client.Value;
            await client.Shutdown();
        }

        void ILanguageServerNotificationListener.OnNotificationSent()
        {
            Interlocked.Increment(ref _serverNotificationSent);
        }

        void ILanguageServerNotificationListener.OnNotificationHandled()
        {
            if (Interlocked.Increment(ref _serverNotificationHandled) == _clientNotificationSent &&
                _clientNotificationReceived == _serverNotificationSent)
            {
                _notificationSync.TrySetResult();
            }
        }

        private void BeforeSendNotification()
        {
            Interlocked.Increment(ref _clientNotificationSent);
            _notificationSync = new TaskCompletionSource();
            _clientNotificationReceivedBeforeSync = _clientNotificationReceived;
        }

        private Task SynchronizeNotifications()
        {
            return _notificationSync.Task;
        }

        private void OnNotification()
        {
            if (Interlocked.Increment(ref _clientNotificationReceived) == _serverNotificationSent &&
                _clientNotificationSent == _serverNotificationHandled)
            {
                _notificationSync.TrySetResult();
            }
        }

        private DocumentUri ToUri(string file)
        {
            return DocumentUri.File(Path.Combine(_workingDirectory, file));
        }

        private string ToFile(DocumentUri uri)
        {
            var path = DocumentUri.GetFileSystemPath(uri);
            return path is null ? uri.ToString() : PathUtility.NormalizeFile(Path.GetRelativePath(_workingDirectory, path));
        }

        private async Task<ILanguageClient> InitializeClient(string workingDirectory, Package package)
        {
            var clientPipe = new Pipe();
            var serverPipe = new Pipe();

            var client = LanguageClient.Create(options => options
                .WithInput(serverPipe.Reader)
                .WithOutput(clientPipe.Writer)
                .WithCapability(new WorkspaceEditCapability()
                {
                    DocumentChanges = true,
                    FailureHandling = FailureHandlingKind.Undo,
                })
                .OnLogMessage(message =>
                {
                    if (message.Type == MessageType.Error)
                    {
                        _notificationSync.TrySetException(new InvalidOperationException(message.Message));
                    }
                })
                .OnPublishDiagnostics(item =>
                {
                    _diagnostics[ToFile(item.Uri)] = JToken.FromObject(item.Diagnostics, JsonUtility.Serializer);
                    OnNotification();
                }));

            Task.Run(() => LanguageServerHost.RunLanguageServer(workingDirectory, new(), clientPipe.Reader, serverPipe.Writer, package, this)).GetAwaiter();

            await client.Initialize(default);

            return client;
        }

        private static JsonDiff CreateLanguageServerJsonDiff()
        {
            return new JsonDiffBuilder()
                .UseAdditionalProperties()
                .UseNegate()
                .UseWildcard()
                .Build();
        }
    }
}
